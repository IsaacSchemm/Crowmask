using Crowmask.Data;
using Crowmask.HighLevel.ATProto;
using Crowmask.LowLevel;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Crowmask.HighLevel
{
    /// <summary>
    /// Accesses and updates cached submission information in the Crowmask database.
    /// </summary>
    public class SubmissionCache(
        ActivityPubTranslator translator,
        BlueskyAgent blueskyAgent,
        IDbContextFactory<CrowmaskDbContext> contextFactory,
        IHttpClientFactory httpClientFactory,
        RemoteInboxLocator inboxLocator,
        WeasylClient weasylClient)
    {
        /// <summary>
        /// Finds the Content-Type of a remote URL.
        /// </summary>
        /// <param name="url">A remote URL</param>
        /// <returns>The media type, or application/octet-stream if the media type could not be determined</returns>
        private async Task<string> GetContentTypeAsync(string url)
        {
            using var httpClient = httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Head, url);
            using var resp = await httpClient.SendAsync(req);
            MediaTypeHeaderValue? val = resp.IsSuccessStatusCode
                ? resp.Content.Headers.ContentType
                : null;
            return val?.MediaType ?? "application/octet-stream";
        }

        /// <summary>
        /// Checks the information for the given post in Crowmask's database
        /// (if any), and fetches new information from Weasyl if the post is
        /// stale or absent. New, updated, and deleted posts will generate new
        /// ActivityPub messages.
        /// </summary>
        /// <param name="postid">The post ID</param>
        /// <returns>A CacheResult union, with the found post if any</returns>
        public async Task<CacheResult> RefreshPostAsync(PostId postId)
        {
            return postId is PostId.SubmitId submitid ? await RefreshSubmissionAsync(submitid.Item)
                : postId is PostId.JournalId journalid ? await RefreshJournalAsync(journalid.Item)
                : throw new NotImplementedException();
        }

        /// <summary>
        /// Checks the information for the given submission in Crowmask's
        /// database (if any), and fetches new information from Weasyl if the
        /// submission is stale or absent. New, updated, and deleted posts will generate new ActivityPub messages.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <param name="force">Whether to force a refresh, even if not stale</param>
        /// <param name="altText">New alt text to apply (will always force a refresh)</param>
        /// <returns>A CacheResult union, with the found post if any</returns>
        public async Task<CacheResult> RefreshSubmissionAsync(int submitid, bool force = false, string? altText = null)
        {
            var Context = await contextFactory.CreateDbContextAsync();

            var cachedSubmission = await Context.Submissions
                .Where(s => s.SubmitId == submitid)
                .SingleOrDefaultAsync();

            if (cachedSubmission != null)
            {
                bool mustRefresh = FreshnessDeterminer.IsStale(cachedSubmission) || force || altText != null;
                if (!mustRefresh)
                    return CacheResult.NewPostResult(Domain.AsPost(cachedSubmission));

                cachedSubmission.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
                await Context.SaveChangesAsync();
            }

            try
            {
                var option = await weasylClient.GetMyPublicSubmissionAsync(submitid);
                if (OptionModule.ToObj(option) is Weasyl.SubmissionDetail weasylSubmission)
                {
                    bool newlyCreated = false;

                    if (cachedSubmission == null)
                    {
                        newlyCreated = true;
                        cachedSubmission = new Submission
                        {
                            SubmitId = submitid,
                            FirstCachedAt = DateTimeOffset.UtcNow
                        };
                        Context.Submissions.Add(cachedSubmission);
                    }

                    var oldSubmission = Domain.AsPost(cachedSubmission);

                    cachedSubmission.Description = weasylSubmission.description;
                    cachedSubmission.Media = await weasylSubmission.media.submission
                        .ToAsyncEnumerable()
                        .SelectAwait(async s => new Submission.SubmissionMedia
                        {
                            Url = s.url,
                            ContentType = await GetContentTypeAsync(s.url)
                        })
                        .ToListAsync();
                    cachedSubmission.Thumbnails = await weasylSubmission.media.thumbnail
                        .ToAsyncEnumerable()
                        .SelectAwait(async s => new Submission.SubmissionMedia
                        {
                            Url = s.url,
                            ContentType = await GetContentTypeAsync(s.url)
                        })
                        .ToListAsync();
                    cachedSubmission.PostedAt = weasylSubmission.posted_at;
                    cachedSubmission.Rating = weasylSubmission.rating;
                    cachedSubmission.Subtype = weasylSubmission.subtype;
                    cachedSubmission.Tags = weasylSubmission.tags
                        .Select(t => new Submission.SubmissionTag
                        {
                            Tag = t
                        })
                        .ToList();
                    cachedSubmission.Title = weasylSubmission.title;

                    cachedSubmission.Link = weasylSubmission.link;
                    cachedSubmission.AltText = altText ?? cachedSubmission.AltText;

                    var newSubmission = Domain.AsPost(cachedSubmission);

                    TimeSpan age = DateTimeOffset.UtcNow - newSubmission.first_upstream;

                    bool changed = !oldSubmission.Equals(newSubmission);
                    bool backfill = newlyCreated && age > TimeSpan.FromHours(12);

                    // Notify ActivityPub servers of updates to posts Crowmask
                    // has seen before, and of posts that are new to Crowmask
                    // that are less than 12 hours old.
                    if (changed && !backfill)
                    {
                        foreach (string inbox in await inboxLocator.GetDistinctInboxesAsync(followersOnly: newlyCreated))
                        {
                            Context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = inbox,
                                JsonBody = ActivityPubSerializer.SerializeWithContext(
                                    newlyCreated
                                    ? translator.ObjectToCreate(newSubmission)
                                    : translator.ObjectToUpdate(newSubmission)),
                                StoredAt = DateTimeOffset.UtcNow
                            });
                        }
                    }

                    cachedSubmission.CacheRefreshSucceededAt = DateTimeOffset.UtcNow;
                    await Context.SaveChangesAsync();

                    if (changed)
                    {
                        await blueskyAgent.TryDeleteBlueskyPostsAsync(cachedSubmission);
                    }
                    await blueskyAgent.TryCreateBlueskyPostsAsync(cachedSubmission);
                    await Context.SaveChangesAsync();

                    return CacheResult.NewPostResult(newSubmission);
                }
                else
                {
                    if (cachedSubmission != null)
                    {
                        Context.Submissions.Remove(cachedSubmission);

                        var post = Domain.AsPost(cachedSubmission);

                        foreach (string inbox in await inboxLocator.GetDistinctInboxesAsync())
                        {
                            Context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = inbox,
                                JsonBody = ActivityPubSerializer.SerializeWithContext(
                                    translator.ObjectToDelete(
                                        post)),
                                StoredAt = DateTimeOffset.UtcNow
                            });
                        }

                        await blueskyAgent.TryDeleteBlueskyPostsAsync(cachedSubmission);

                        await Context.SaveChangesAsync();
                    }

                    return CacheResult.PostNotFound;
                }
            }
            catch (HttpRequestException)
            {
                return cachedSubmission == null
                    ? CacheResult.PostNotFound
                    : CacheResult.NewPostResult(Domain.AsPost(cachedSubmission));
            }
        }

        /// <summary>
        /// Checks the information for the given journal entry in Crowmask's
        /// database (if any), and fetches new information from Weasyl if the
        /// journal entry is stale or absent. New, updated, and deleted posts
        /// will generate new ActivityPub messages.
        /// </summary>
        /// <param name="journalid">The journal ID</param>
        /// <param name="force">Whether to force a refresh, even if not stale</param>
        /// <returns>A CacheResult union, with the found post if any</returns>
        public async Task<CacheResult> RefreshJournalAsync(int journalid, bool force = false)
        {
            var Context = await contextFactory.CreateDbContextAsync();

            var cachedJournal = await Context.Journals
                .Where(j => j.JournalId == journalid)
                .SingleOrDefaultAsync();

            if (cachedJournal != null)
            {
                bool mustRefresh = FreshnessDeterminer.IsStale(cachedJournal) || force;
                if (!mustRefresh)
                    return CacheResult.NewPostResult(Domain.JournalAsPost(cachedJournal));

                cachedJournal.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
                await Context.SaveChangesAsync();
            }

            try
            {
                var option = await weasylClient.GetMyPublicJournalAsync(journalid);
                if (OptionModule.ToObj(option) is Weasyl.JournalDetail weasylJournal)
                {
                    bool newlyCreated = false;

                    if (cachedJournal == null)
                    {
                        newlyCreated = true;
                        cachedJournal = new Journal
                        {
                            JournalId = journalid,
                            FirstCachedAt = DateTimeOffset.UtcNow
                        };
                        Context.Journals.Add(cachedJournal);
                    }

                    var oldJournal = Domain.JournalAsPost(cachedJournal);

                    cachedJournal.Content = weasylJournal.content;
                    cachedJournal.PostedAt = weasylJournal.posted_at;
                    cachedJournal.Rating = weasylJournal.rating;
                    cachedJournal.Tags = weasylJournal.tags
                        .Select(t => new Journal.JournalTag
                        {
                            Tag = t
                        })
                        .ToList();
                    cachedJournal.Title = weasylJournal.title;

                    cachedJournal.Link = weasylJournal.link;

                    var newJournal = Domain.JournalAsPost(cachedJournal);

                    TimeSpan age = DateTimeOffset.UtcNow - newJournal.first_upstream;

                    bool changed = !oldJournal.Equals(newJournal);
                    bool backfill = newlyCreated && age > TimeSpan.FromHours(12);

                    // Notify ActivityPub servers of updates to posts Crowmask
                    // has seen before, and of posts that are new to Crowmask
                    // that are less than 12 hours old.
                    if (changed && !backfill)
                    {
                        foreach (string inbox in await inboxLocator.GetDistinctInboxesAsync(followersOnly: newlyCreated))
                        {
                            Context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = inbox,
                                JsonBody = ActivityPubSerializer.SerializeWithContext(
                                    newlyCreated
                                    ? translator.ObjectToCreate(newJournal)
                                    : translator.ObjectToUpdate(newJournal)),
                                StoredAt = DateTimeOffset.UtcNow
                            });
                        }
                    }

                    cachedJournal.CacheRefreshSucceededAt = DateTimeOffset.UtcNow;
                    await Context.SaveChangesAsync();

                    await Context.SaveChangesAsync();

                    return CacheResult.NewPostResult(newJournal);
                }
                else
                {
                    if (cachedJournal != null)
                    {
                        Context.Journals.Remove(cachedJournal);

                        var post = Domain.JournalAsPost(cachedJournal);

                        foreach (string inbox in await inboxLocator.GetDistinctInboxesAsync())
                        {
                            Context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = inbox,
                                JsonBody = ActivityPubSerializer.SerializeWithContext(
                                    translator.ObjectToDelete(
                                        post)),
                                StoredAt = DateTimeOffset.UtcNow
                            });
                        }

                        await Context.SaveChangesAsync();
                    }

                    return CacheResult.PostNotFound;
                }
            }
            catch (HttpRequestException)
            {
                return cachedJournal == null
                    ? CacheResult.PostNotFound
                    : CacheResult.NewPostResult(Domain.JournalAsPost(cachedJournal));
            }
        }

        /// <summary>
        /// Gets cached submission information from Crowmask's database
        /// without making an upstream call to Weasyl, even if the information
        /// is stale or absent.
        /// </summary>
        /// <param name="submitid"></param>
        /// <returns></returns>
        public async Task<CacheResult> GetCachedSubmissionAsync(int submitid)
        {
            var Context = await contextFactory.CreateDbContextAsync();

            var cachedSubmission = await Context.Submissions
                .Where(s => s.SubmitId == submitid)
                .SingleOrDefaultAsync();
            return cachedSubmission != null
                ? CacheResult.NewPostResult(Domain.AsPost(cachedSubmission))
                : CacheResult.PostNotFound;
        }

        /// <summary>
        /// Gets cached journal entry information from Crowmask's database
        /// without making an upstream call to Weasyl, even if the information
        /// is stale or absent.
        /// </summary>
        /// <param name="journalid"></param>
        /// <returns></returns>
        public async Task<CacheResult> GetCachedJournalAsync(int journalid)
        {
            var Context = await contextFactory.CreateDbContextAsync();

            var cachedSubmission = await Context.Journals
                .Where(j => j.JournalId == journalid)
                .SingleOrDefaultAsync();
            return cachedSubmission != null
                ? CacheResult.NewPostResult(Domain.JournalAsPost(cachedSubmission))
                : CacheResult.PostNotFound;
        }

        /// <summary>
        /// Returns a list of cached submissions from Crowmask's database.
        /// Submissions with higher IDs will be returned first.
        /// </summary>
        /// <param name="nextid">Only return submissions with an ID lower than this one</param>
        /// <param name="since">Only return submissions originally posted to Weasyl after this point in time</param>
        /// <returns>A list of submissions</returns>
        public async IAsyncEnumerable<Post> GetCachedSubmissionsAsync(int nextid = int.MaxValue, DateTimeOffset? since = null)
        {
            var Context = await contextFactory.CreateDbContextAsync();

            DateTimeOffset cutoff = since ?? DateTimeOffset.MinValue;

            var query = Context.Submissions
                .Where(s => s.SubmitId < nextid)
                .Where(s => s.PostedAt > cutoff)
                .OrderByDescending(s => s.SubmitId)
                .AsAsyncEnumerable();

            await foreach (var submission in query)
                yield return Domain.AsPost(submission);
        }

        /// <summary>
        /// Returns a list of cached journal entries from Crowmask's database.
        /// Journal entries with higher IDs will be returned first.
        /// </summary>
        /// <param name="nextid">Only return journal entries with an ID lower than this one</param>
        /// <param name="since">Only return journal entries originally posted to Weasyl after this point in time</param>
        /// <returns>A list of journal entries</returns>
        public async IAsyncEnumerable<Post> GetCachedJournalsAsync(int nextid = int.MaxValue, DateTimeOffset? since = null)
        {
            var Context = await contextFactory.CreateDbContextAsync();

            DateTimeOffset cutoff = since ?? DateTimeOffset.MinValue;

            var query = Context.Journals
                .Where(j => j.JournalId < nextid)
                .Where(j => j.PostedAt > cutoff)
                .OrderByDescending(j => j.JournalId)
                .AsAsyncEnumerable();

            await foreach (var journal in query)
                yield return Domain.JournalAsPost(journal);
        }

        /// <summary>
        /// Returns the current number of cached submissions and journal
        /// entries in Crowmask's database.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCachedPostCountAsync()
        {
            var Context = await contextFactory.CreateDbContextAsync();

            return await Context.Submissions.CountAsync()
                + await Context.Journals.CountAsync();
        }

        /// <summary>
        /// Returns a combined list of cached submissions and journal entries
        /// from Crowmask's database.
        /// </summary>
        /// <returns>A list of posts</returns>
        public IAsyncEnumerable<Post> GetCachedPostsAsync()
        {
            return new[]
            {
                GetCachedSubmissionsAsync(),
                GetCachedJournalsAsync()
            }.MergeNewest(post => post.first_cached);
        }
    }
}
