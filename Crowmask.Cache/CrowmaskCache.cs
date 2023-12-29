using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Weasyl;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace Crowmask.Cache
{
    public class CrowmaskCache(CrowmaskDbContext Context, IHttpClientFactory httpClientFactory, IPublicKeyProvider KeyProvider, Translator Translator, WeasylClient WeasylClient)
    {
        private class SubmissionGoneException : Exception { }

        private async Task<WeasylSubmissionDetail> GetSubmissionAsync(int submitid)
        {
            try
            {
                var weasylSubmission = await WeasylClient.GetSubmissionAsync(submitid);

                if (weasylSubmission.friends_only)
                    throw new SubmissionGoneException();

                return weasylSubmission;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new SubmissionGoneException();
            }
        }

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

        public async Task<Post?> GetSubmission(int submitid)
        {
            var cachedSubmission = await Context.Submissions
                .Include(s => s.Media)
                .Include(s => s.Tags)
                .Where(s => s.SubmitId == submitid)
                .SingleOrDefaultAsync();

            if (cachedSubmission != null)
            {
                if (!cachedSubmission.Stale)
                    return Domain.AsNote(cachedSubmission);

                cachedSubmission.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
                await Context.SaveChangesAsync();
            }

            try
            {
                WeasylSubmissionDetail weasylSubmission = await GetSubmissionAsync(submitid);

                bool newlyCreated = false;

                if (cachedSubmission == null)
                {
                    var whoami = await WeasylClient.WhoamiAsync();
                    if (whoami.login != weasylSubmission.owner)
                        return null;

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
                cachedSubmission.FriendsOnly = weasylSubmission.friends_only;
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
                cachedSubmission.RatingId = weasylSubmission.rating switch
                {
                    "general" => Submission.Rating.General,
                    "moderate" => Submission.Rating.Moderate,
                    "mature" => Submission.Rating.Mature,
                    "explicit" => Submission.Rating.Explicit,
                    _ => 0
                };
                cachedSubmission.SubtypeId = weasylSubmission.subtype switch
                {
                    "visual" => Submission.Subtype.Visual,
                    "literary" => Submission.Subtype.Literary,
                    "multimedia" => Submission.Subtype.Multimedia,
                    _ => 0
                };
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
                    var followers = await Context.Followers.ToListAsync();
                    var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);

                    foreach (var inbox in inboxes)
                    {
                        Context.OutboundActivities.Add(new OutboundActivity
                        {
                            Id = Guid.NewGuid(),
                            Inbox = inbox.Key,
                            JsonBody = AP.SerializeWithContext(
                                newlyCreated
                                ? Translator.ObjectToCreate(newSubmission)
                                : Translator.ObjectToUpdate(newSubmission)),
                            StoredAt = DateTimeOffset.UtcNow
                        });
                    }
                }

                cachedSubmission.CacheRefreshSucceededAt = DateTimeOffset.UtcNow;
                await Context.SaveChangesAsync();

                return newSubmission;
            }
            catch (SubmissionGoneException)
            {
                if (cachedSubmission != null)
                {
                    Context.Submissions.Remove(cachedSubmission);

                    var followers = await Context.Followers.ToListAsync();
                    var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);

                    foreach (var inbox in inboxes)
                    {
                        Context.OutboundActivities.Add(new OutboundActivity
                        {
                            Id = Guid.NewGuid(),
                            Inbox = inbox.Key,
                            JsonBody = AP.SerializeWithContext(Translator.ObjectToDelete(cachedSubmission.SubmitId)),
                            StoredAt = DateTimeOffset.UtcNow
                        });
                    }

                    await Context.SaveChangesAsync();
                }

                return null;
            }
            catch (HttpRequestException)
            {
                return cachedSubmission == null
                    ? null
                    : Domain.AsNote(cachedSubmission);
            }
        }

        public async Task<Person> GetUser()
        {
            var cachedUser = await Context.GetUserAsync();

            if (DateTimeOffset.UtcNow - cachedUser.CacheRefreshSucceededAt < TimeSpan.FromHours(1))
                return Domain.AsPerson(cachedUser);

            if (DateTimeOffset.UtcNow - cachedUser.CacheRefreshAttemptedAt < TimeSpan.FromMinutes(5))
                return Domain.AsPerson(cachedUser);

            cachedUser.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
            await Context.SaveChangesAsync();

            var whoami = await WeasylClient.WhoamiAsync();

            var weasylUser = await WeasylClient.GetUserAsync(whoami.login);

            var oldUser = Domain.AsPerson(cachedUser);

            cachedUser.Username = weasylUser.username;
            cachedUser.FullName = weasylUser.full_name;
            cachedUser.ProfileText = weasylUser.profile_text;
            cachedUser.Url = weasylUser.link;
            cachedUser.Avatars = weasylUser.media.avatar
                .Select(a => new UserAvatar
                {
                    Id = Guid.NewGuid(),
                    Url = a.url,
                    MediaId = a.mediaid
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

                var followers = await Context.Followers.ToListAsync();

                var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);
                foreach (var inbox in inboxes)
                {
                    Context.OutboundActivities.Add(new OutboundActivity
                    {
                        Id = Guid.NewGuid(),
                        Inbox = inbox.Key,
                        JsonBody = AP.SerializeWithContext(Translator.PersonToUpdate(newUser, key)),
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
