using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Merging;
using Crowmask.Weasyl;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace Crowmask.Cache
{
    public class CrowmaskCache(CrowmaskDbContext Context, IHttpClientFactory httpClientFactory, IInteractionLookup interactionLookup, IPublicKeyProvider KeyProvider, Translator Translator, WeasylUserClient weasylUserClient)
    {
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
            return cachedSubmission != null
                ? CacheResult.NewPostResult(Domain.AsNote(cachedSubmission))
                : await UpdateSubmissionAsync(submitid);
        }

        public async Task<CacheResult> UpdateSubmissionAsync(int submitid)
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
                WeasylSubmissionDetail weasylSubmission = await weasylUserClient.GetMyPublicSubmissionAsync(submitid);
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

                    return CacheResult.NewPostResult(newSubmission);
                }
                else
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
                                JsonBody = AP.SerializeWithContext(Translator.ObjectToDelete(Domain.AsNote(cachedSubmission))),
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

        public async IAsyncEnumerable<Post> GetRelevantSubmissionsAsync(string activity_or_reply_id)
        {
            var ids = await interactionLookup
                .GetRelevantSubmitIdsAsync(activity_or_reply_id)
                .ToListAsync();
            foreach (int id in ids)
                if (await GetSubmissionAsync(id) is CacheResult.PostResult pr)
                    yield return pr.Post;
        }

        public async Task<CacheResult> GetJournalAsync(int journalid)
        {
            var cachedJournal = await Context.Journals
                .Where(s => s.JournalId == journalid)
                .SingleOrDefaultAsync();
            return cachedJournal != null
                ? CacheResult.NewPostResult(Domain.AsArticle(cachedJournal))
                : await UpdateJournalAsync(journalid);
        }

        public async Task<CacheResult> UpdateJournalAsync(int journalid)
        {
            var cachedJournal = await Context.Journals
                .Where(s => s.JournalId == journalid)
                .SingleOrDefaultAsync();

            if (cachedJournal != null)
            {
                if (!cachedJournal.Stale)
                    return CacheResult.NewPostResult(Domain.AsArticle(cachedJournal));

                cachedJournal.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
                await Context.SaveChangesAsync();
            }

            try
            {
                var whoami = await weasylUserClient.GetMyUserAsync();

                var weasylJournal = await weasylUserClient.GetMyJournalAsync(journalid);
                if (weasylJournal != null)
                {
                    bool newlyCreated = false;

                    if (cachedJournal == null)
                    {
                        newlyCreated = true;
                        cachedJournal = new Journal
                        {
                            JournalId = weasylJournal.JournalId,
                            FirstCachedAt = DateTimeOffset.UtcNow
                        };
                        Context.Journals.Add(cachedJournal);
                    }

                    var oldJournal = Domain.AsArticle(cachedJournal);

                    cachedJournal.Content = weasylJournal.Content;
                    cachedJournal.PostedAt = weasylJournal.PostedAt;
                    cachedJournal.Rating = weasylJournal.Rating;
                    cachedJournal.Title = weasylJournal.Title;
                    cachedJournal.VisibilityRestricted = weasylJournal.VisibilityRestricted;

                    var newJournal = Domain.AsArticle(cachedJournal);

                    TimeSpan age = DateTimeOffset.UtcNow - newJournal.first_upstream;

                    bool changed = !oldJournal.Equals(newJournal);
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
                                    ? Translator.ObjectToCreate(newJournal)
                                    : Translator.ObjectToUpdate(newJournal)),
                                StoredAt = DateTimeOffset.UtcNow
                            });
                        }
                    }

                    cachedJournal.CacheRefreshSucceededAt = DateTimeOffset.UtcNow;
                    await Context.SaveChangesAsync();

                    return CacheResult.NewPostResult(newJournal);
                }
                else
                {
                    if (cachedJournal != null)
                    {
                        Context.Journals.Remove(cachedJournal);

                        var followers = await Context.Followers.ToListAsync();
                        var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);

                        foreach (var inbox in inboxes)
                        {
                            Context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = inbox.Key,
                                JsonBody = AP.SerializeWithContext(Translator.ObjectToDelete(Domain.AsArticle(cachedJournal))),
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
                return cachedJournal == null
                    ? CacheResult.NotFound
                    : CacheResult.NewPostResult(Domain.AsArticle(cachedJournal));
            }
        }

        public async IAsyncEnumerable<Post> GetCachedJournalsAsync()
        {
            int last = int.MaxValue;
            while (true)
            {
                var journals = await Context.Journals
                    .Where(j => j.JournalId < last)
                    .OrderByDescending(j => j.JournalId)
                    .Take(20)
                    .ToListAsync();

                foreach (var journal in journals)
                    yield return Domain.AsArticle(journal);

                if (journals.Count == 0)
                    break;

                last = journals.Select(j => j.JournalId).Min();
            }
        }

        public async IAsyncEnumerable<Post> GetRelevantJournalsAsync(string activity_or_reply_id)
        {
            var ids = await interactionLookup
                .GetRelevantJournalIdsAsync(activity_or_reply_id)
                .ToListAsync();
            foreach (int id in ids)
                if (await GetJournalAsync(id) is CacheResult.PostResult pr)
                    yield return pr.Post;
        }

        public async Task<CacheResult> GetCachedPostAsync(JointIdentifier identifier)
        {
            if (identifier.IsSubmissionIdentifier)
                return await GetSubmissionAsync(identifier.submitid);
            else if (identifier.IsJournalIdentifier)
                return await GetJournalAsync(identifier.journalid);
            else
                return CacheResult.NotFound;
        }

        public async Task<int> GetCachedPostCountAsync()
        {
            int submissions = await Context.Submissions.CountAsync();
            int journals = await Context.Journals.CountAsync();
            return submissions + journals;
        }

        public IAsyncEnumerable<Post> GetAllCachedPostsAsync()
        {
            return new[] {
                GetCachedSubmissionsAsync(),
                GetCachedJournalsAsync()
            }
            .MergeNewest(post => post.first_upstream);
        }

        public async IAsyncEnumerable<Post> GetRelevantCachedPostsAsync(string activity_or_reply_id)
        {
            await foreach (var post in GetRelevantSubmissionsAsync(activity_or_reply_id))
                yield return post;
            await foreach (var post in GetRelevantJournalsAsync(activity_or_reply_id))
                yield return post;
        }

        public async Task<Person> GetUserAsync()
        {
            var cachedUser = await Context.GetUserAsync();
            return cachedUser == null
                ? await UpdateUserAsync()
                : Domain.AsPerson(cachedUser);
        }

        public async Task<Person> UpdateUserAsync()
        {
            var cachedUser = await Context.GetUserAsync();

            if (DateTimeOffset.UtcNow - cachedUser.CacheRefreshSucceededAt < TimeSpan.FromHours(1))
                return Domain.AsPerson(cachedUser);

            if (DateTimeOffset.UtcNow - cachedUser.CacheRefreshAttemptedAt < TimeSpan.FromMinutes(5))
                return Domain.AsPerson(cachedUser);

            cachedUser.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
            await Context.SaveChangesAsync();

            var weasylUser = await weasylUserClient.GetMyUserAsync();

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
