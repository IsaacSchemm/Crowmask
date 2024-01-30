using Crowmask.Data;
using Crowmask.LowLevel;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using System.Net.Http.Headers;

namespace Crowmask.HighLevel
{
    /// <summary>
    /// Accesses and updates cached submission information in the Crowmask database.
    /// </summary>
    public class SubmissionCache(
        IdMapper idMapper,
        ActivityPubTranslator translator,
        CrowmaskDbContext Context,
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
        /// Checks the information for the given submission in Crowmask's
        /// database (if any), and fetches new information from Weasyl if the
        /// submission is stale or absent. New, updated, and deleted postswill generate new ActivityPub messages.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <returns>A CacheResult union, with the found post if any</returns>
        public async Task<CacheResult> RefreshSubmissionAsync(int submitid)
        {
            var cachedSubmission = await Context.Submissions
                .Where(s => s.SubmitId == submitid)
                .SingleOrDefaultAsync();

            if (cachedSubmission != null)
            {
                if (!cachedSubmission.Stale)
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
                        .SelectAwait(async s => new Submission.SubmissionThumbnail
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

                    var newSubmission = Domain.AsPost(cachedSubmission);

                    TimeSpan age = DateTimeOffset.UtcNow - newSubmission.first_upstream;

                    bool changed = !oldSubmission.Equals(newSubmission);
                    bool backfill = newlyCreated && age > TimeSpan.FromHours(12);

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

                        var mentions = await Context.Mentions
                            .Where(x => x.ObjectId == idMapper.GetObjectId(cachedSubmission.SubmitId))
                            .ToListAsync();
                        foreach (var mention in mentions)
                        {
                            Context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = await inboxLocator.GetAdminActorInboxAsync(),
                                JsonBody = ActivityPubSerializer.SerializeWithContext(
                                    translator.PrivateNoteToDelete(
                                        mention)),
                                StoredAt = DateTimeOffset.UtcNow
                            });
                        }

                        var interactions = await Context.Interactions
                            .Where(x => x.TargetId == idMapper.GetObjectId(cachedSubmission.SubmitId))
                            .ToListAsync();
                        foreach (var interaction in interactions)
                        {
                            Context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = await inboxLocator.GetAdminActorInboxAsync(),
                                JsonBody = ActivityPubSerializer.SerializeWithContext(
                                    translator.PrivateNoteToDelete(
                                        interaction)),
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
                return cachedSubmission == null
                    ? CacheResult.PostNotFound
                    : CacheResult.NewPostResult(Domain.AsPost(cachedSubmission));
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
            var cachedSubmission = await Context.Submissions
                .Where(s => s.SubmitId == submitid)
                .SingleOrDefaultAsync();
            return cachedSubmission != null
                ? CacheResult.NewPostResult(Domain.AsPost(cachedSubmission))
                : CacheResult.PostNotFound;
        }

        /// <summary>
        /// Returns a list of cached submissions from Crowmask's database.
        /// Submissions with higher IDs will be returned first.
        /// </summary>
        /// <param name="nextid">Only return submissions with an ID lower than this one</param>
        /// <param name="since">Only return submissions originally posted to Weasyl after this point in time</param>
        /// <returns>A list of submission IDs</returns>
        public async IAsyncEnumerable<Post> GetCachedSubmissionsAsync(int nextid = int.MaxValue, DateTimeOffset? since = null)
        {
            DateTimeOffset cutoff = since ?? DateTimeOffset.MinValue;
            int batchSize = 5;

            while (true)
            {
                var list = await Context.Submissions
                    .Where(s => s.SubmitId < nextid)
                    .Where(s => s.PostedAt > cutoff)
                    .OrderByDescending(s => s.SubmitId)
                    .Take(batchSize)
                    .ToListAsync();

                foreach (var submission in list)
                    yield return Domain.AsPost(submission);

                if (list.Count == 0)
                    break;

                nextid = list.Last().SubmitId;
                batchSize = 100;
            }
        }

        /// <summary>
        /// Returns the current number of cached posts in Crowmask's database.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCachedSubmissionCountAsync()
        {
            return await Context.Submissions.CountAsync();
        }
    }
}
