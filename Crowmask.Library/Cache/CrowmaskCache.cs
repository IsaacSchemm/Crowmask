using Crowmask.Data;
using Crowmask.Dependencies.Weasyl;
using Crowmask.DomainModeling;
using Crowmask.Formats;
using Crowmask.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace Crowmask.Library.Cache
{
    /// <summary>
    /// Accesses cached posts and user information from the Crowmask database,
    /// and refreshes cached information from Weasyl when stale or missing.
    /// </summary>
    public class CrowmaskCache(CrowmaskDbContext Context, IHttpClientFactory httpClientFactory, IInteractionLookup interactionLookup, ICrowmaskKeyProvider KeyProvider, ActivityPubTranslator translator, WeasylClient weasylClient)
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
        /// Gets a set of ActivityPub inboxes to send a message to.
        /// </summary>
        /// <param name="followersOnly">Whether to limit the message to only followers' servers. If false, Crowmask will include all known servers.</param>
        /// <returns>A set of inbox URLs</returns>
        private async Task<IReadOnlySet<string>> GetDistinctInboxesAsync(bool followersOnly = false)
        {
            async IAsyncEnumerable<string> enumerate()
            {
                // Go through follower inboxes first - prefer shared inbox if present
                await foreach (var follower in Context.Followers.AsAsyncEnumerable())
                    yield return follower.SharedInbox ?? follower.Inbox;

                // Then include all other known inboxes, if enabled
                if (!followersOnly)
                    await foreach (var known in Context.KnownInboxes.AsAsyncEnumerable())
                        yield return known.Inbox;
            }

            // Combine results above into an unordered set of unique items
            return await enumerate().ToHashSetAsync();
        }

        /// <summary>
        /// Pulls a submission from Crowmask's database, or attempts a refresh
        /// from Weasyl if the submission is stale or absent. New, updated,
        /// and deleted posts will generate new ActivityPub messages.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <returns>A CacheResult union, with the found post if any</returns>
        public async Task<CacheResult> GetSubmissionAsync(int submitid)
        {
            var cachedSubmission = await Context.Submissions
                .Include(s => s.Boosts)
                .Include(s => s.Likes)
                .Include(s => s.Replies)
                .Include(s => s.Media)
                .Include(s => s.Tags)
                .Where(s => s.SubmitId == submitid)
                .SingleOrDefaultAsync();

            if (cachedSubmission != null)
            {
                if (!cachedSubmission.Stale)
                    return CacheResult.NewPostResult(Domain.AsNote(cachedSubmission));

                cachedSubmission.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
                await Context.SaveChangesAsync();
            }

            try
            {
                WeasylSubmissionDetail? weasylSubmission = await weasylClient.GetMyPublicSubmissionAsync(submitid);
                if (weasylSubmission != null)
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

                    var oldSubmission = Domain.AsNote(cachedSubmission);

                    cachedSubmission.Description = weasylSubmission.description;
                    cachedSubmission.Media = await weasylSubmission.media.submission
                        .ToAsyncEnumerable()
                        .SelectAwait(async s => new SubmissionMedia
                        {
                            Id = Guid.NewGuid(),
                            Url = s.url,
                            ContentType = await GetContentTypeAsync(s.url)
                        })
                        .ToListAsync();
                    cachedSubmission.Thumbnails = await weasylSubmission.media.thumbnail
                        .ToAsyncEnumerable()
                        .SelectAwait(async s => new SubmissionThumbnail
                        {
                            Id = Guid.NewGuid(),
                            Url = s.url,
                            ContentType = await GetContentTypeAsync(s.url)
                        })
                        .ToListAsync();
                    cachedSubmission.PostedAt = weasylSubmission.posted_at;
                    cachedSubmission.Rating = weasylSubmission.rating;
                    cachedSubmission.Tags = weasylSubmission.tags
                        .Select(t => new SubmissionTag
                        {
                            Id = Guid.NewGuid(),
                            Tag = t
                        })
                        .ToList();
                    cachedSubmission.Title = weasylSubmission.title;
                    cachedSubmission.Link = weasylSubmission.link;

                    var newSubmission = Domain.AsNote(cachedSubmission);

                    TimeSpan age = DateTimeOffset.UtcNow - newSubmission.first_upstream;

                    bool changed = !oldSubmission.Equals(newSubmission);
                    bool backfill = newlyCreated && age > TimeSpan.FromHours(12);

                    if (changed && !backfill)
                    {
                        foreach (string inbox in await GetDistinctInboxesAsync(followersOnly: newlyCreated))
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

                        foreach (string inbox in await GetDistinctInboxesAsync())
                        {
                            Context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = inbox,
                                JsonBody = ActivityPubSerializer.SerializeWithContext(
                                    translator.ObjectToDelete(
                                        Domain.AsNote(cachedSubmission))),
                                StoredAt = DateTimeOffset.UtcNow
                            });
                        }

                        await Context.SaveChangesAsync();

                        return CacheResult.Deleted;
                    }
                    else
                    {
                        return CacheResult.NotFound;
                    }
                }
            }
            catch (HttpRequestException)
            {
                return cachedSubmission == null
                    ? CacheResult.NotFound
                    : CacheResult.NewPostResult(Domain.AsNote(cachedSubmission));
            }
        }

        /// <summary>
        /// Returns all cached submissions in Crowmask's database, with the
        /// newest submissions (with higher submission IDs) first.
        /// </summary>
        /// <returns>An asynchronous sequence of posts</returns>
        public async IAsyncEnumerable<Post> GetCachedSubmissionsAsync()
        {
            int last = int.MaxValue;
            while (true)
            {
                var submissions = await Context.Submissions
                    .Where(s => s.SubmitId < last)
                    .OrderByDescending(s => s.SubmitId)
                    .Take(20)
                    .ToListAsync();

                foreach (var submission in submissions)
                    yield return Domain.AsNote(submission);

                if (submissions.Count == 0)
                    break;

                last = submissions.Select(s => s.SubmitId).Min();
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

        /// <summary>
        /// Returns the post that the given like, boost, or reply is attached
        /// to, if any.
        /// </summary>
        /// <param name="activity_or_reply_id">The activity ID of the like or boost, or the ID of the reply</param>
        /// <returns>A CacheResult union, with the found post if any</returns>
        public async Task<CacheResult> GetRelevantCachedPostAsync(string activity_or_reply_id)
        {
            if (await interactionLookup.GetRelevantSubmitIdAsync(activity_or_reply_id) is int submitid)
                if (await GetSubmissionAsync(submitid) is CacheResult.PostResult r)
                    return r;

            return CacheResult.NotFound;
        }

        /// <summary>
        /// Pulls user profile information from Crowmask's database, or
        /// attempts a refresh from Weasyl if the information is stale or
        /// absent. Any changes will generate new ActivityPub messages.
        /// </summary>
        /// <returns>A Person object</returns>
        public async Task<Person> GetUserAsync()
        {
            var cachedUser = await Context.GetUserAsync();

            if (!cachedUser.Stale)
                return Domain.AsPerson(cachedUser);

            cachedUser.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
            await Context.SaveChangesAsync();

            var weasylUser = await weasylClient.GetMyUserAsync();

            var oldUser = Domain.AsPerson(cachedUser);

            cachedUser.Username = weasylUser.username;
            cachedUser.FullName = weasylUser.full_name;
            cachedUser.ProfileText = weasylUser.profile_text;
            cachedUser.Url = weasylUser.link;
            cachedUser.Avatars = weasylUser.media.avatar
                .Select(a => new UserAvatar
                {
                    Id = Guid.NewGuid(),
                    Url = a.url
                })
                .ToList();
            cachedUser.Age = weasylUser.user_info.age;
            cachedUser.Gender = weasylUser.user_info.gender;
            cachedUser.Location = weasylUser.user_info.location;

            IEnumerable<UserLink> recreateUserLinks()
            {
                foreach (var pair in weasylUser!.user_info.user_links)
                {
                    foreach (var value in pair.Value)
                    {
                        yield return new UserLink
                        {
                            Id = Guid.NewGuid(),
                            Site = pair.Key,
                            UsernameOrUrl = value
                        };
                    }
                }
            }
            cachedUser.Links = recreateUserLinks().ToList();

            var newUser = Domain.AsPerson(cachedUser);

            if (!oldUser.Equals(newUser))
            {
                var key = await KeyProvider.GetPublicKeyAsync();

                foreach (string inbox in await GetDistinctInboxesAsync())
                {
                    Context.OutboundActivities.Add(new OutboundActivity
                    {
                        Id = Guid.NewGuid(),
                        Inbox = inbox,
                        JsonBody = ActivityPubSerializer.SerializeWithContext(
                            translator.PersonToUpdate(
                                newUser,
                                key)),
                        StoredAt = DateTimeOffset.UtcNow
                    });
                }
            }

            cachedUser.CacheRefreshSucceededAt = DateTimeOffset.UtcNow;
            await Context.SaveChangesAsync();

            return newUser;
        }
    }
}
