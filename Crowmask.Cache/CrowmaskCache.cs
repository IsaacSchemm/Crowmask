using CrosspostSharp3.Weasyl;
using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.Weasyl;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.Cache
{
    public class CrowmaskCache
    {
        private readonly CrowmaskDbContext _context;
        public readonly IKeyProvider _keyProvider;
        private readonly WeasylClient _weasylClient;

        public const int WEASYL_MIRROR_ACTOR = 1;

        public CrowmaskCache(CrowmaskDbContext context, IKeyProvider keyProvider, IWeasylApiKeyProvider apiKeyProvider)
        {
            _context = context;
            _keyProvider = keyProvider;
            _weasylClient = new WeasylClient(apiKeyProvider);
        }

        public async Task<Domain.Note?> GetSubmission(int submitid)
        {
            var cachedSubmission = await _context.Submissions
                .Include(s => s.Media)
                .Include(s => s.Tags)
                .Where(s => s.SubmitId == submitid)
                .SingleOrDefaultAsync();

            if (cachedSubmission != null)
            {
                if (DateTimeOffset.UtcNow - cachedSubmission.CacheRefreshSucceededAt < TimeSpan.FromHours(1))
                    return Domain.AsNote(cachedSubmission);

                if (DateTimeOffset.UtcNow - cachedSubmission.CacheRefreshAttemptedAt < TimeSpan.FromMinutes(5))
                    return Domain.AsNote(cachedSubmission);

                cachedSubmission.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            try
            {
                var weasylSubmission = await _weasylClient.GetSubmissionAsync(submitid);

                bool newlyCreated = false;

                if (cachedSubmission == null)
                {
                    newlyCreated = true;
                    cachedSubmission = new Submission
                    {
                        SubmitId = submitid,
                        FirstCachedAt = DateTimeOffset.UtcNow
                    };
                    _context.Submissions.Add(cachedSubmission);
                }

                var oldSubmission = Domain.AsNote(cachedSubmission);

                cachedSubmission.Description = weasylSubmission.description;
                cachedSubmission.FriendsOnly = weasylSubmission.friends_only;
                cachedSubmission.Media = weasylSubmission.media.submission
                    .Select(s => new SubmissionMedia
                    {
                        Id = Guid.NewGuid(),
                        Url = s.url
                    })
                    .ToList();
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

                if (!oldSubmission.Equals(newSubmission))
                {
                    var followers = await _context.Followers.ToListAsync();
                    var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);

                    foreach (var inbox in inboxes)
                    {
                        Guid guid = Guid.NewGuid();
                        _context.OutboundActivities.Add(new OutboundActivity
                        {
                            Id = Guid.NewGuid(),
                            ExternalId = guid,
                            Inbox = inbox.Key,
                            JsonBody = AP.SerializeWithContext(
                                newlyCreated
                                ? AP.ObjectToCreate(guid, newSubmission)
                                : AP.ObjectToUpdate(guid, newSubmission)),
                            StoredAt = DateTimeOffset.UtcNow
                        });
                    }
                }

                cachedSubmission.CacheRefreshSucceededAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();

                return newSubmission;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (cachedSubmission != null)
                {
                    _context.Submissions.Remove(cachedSubmission);

                    var followers = await _context.Followers.ToListAsync();
                    var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);

                    foreach (var inbox in inboxes)
                    {
                        Guid guid = Guid.NewGuid();
                        _context.OutboundActivities.Add(new OutboundActivity
                        {
                            Id = Guid.NewGuid(),
                            ExternalId = guid,
                            Inbox = inbox.Key,
                            JsonBody = AP.SerializeWithContext(AP.ObjectToDelete(guid, cachedSubmission.SubmitId)),
                            StoredAt = DateTimeOffset.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();
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

        public async IAsyncEnumerable<Domain.Note> GetSubmissionsAsync(int max)
        {
            var mostRecent = await _context.Submissions
                .OrderByDescending(s => s.FirstCachedAt)
                .Select(s => s.SubmitId)
                .Take(max)
                .ToListAsync();

            foreach (int submitId in mostRecent)
            {
                var note = await GetSubmission(submitId);
                if (note != null)
                    yield return note;
            }

            //var whoami = await _weasylClient.WhoamiAsync();

            //int? nextid = null;

            //while (true)
            //{
            //    var gallery = await _weasylClient.GetUserGalleryAsync(whoami.login, new WeasylClient.GalleryRequestOptions
            //    {
            //        nextid = nextid
            //    });

            //    if (!gallery.submissions.Any())
            //        yield break;

            //    foreach (var submission in gallery.submissions)
            //    {
            //        var note = await GetSubmission(submission.submitid);
            //        if (note != null)
            //            yield return note;
            //    }

            //    nextid = gallery.nextid;
            //}
        }

        public async Task<Domain.Person> GetUser()
        {
            var cachedUser = await _context.Users
                .Include(u => u.Avatars)
                .Include(u => u.Links)
                .Where(u => u.UserId == WEASYL_MIRROR_ACTOR)
                .SingleOrDefaultAsync();

            if (cachedUser != null)
            {
                if (DateTimeOffset.UtcNow - cachedUser.CacheRefreshSucceededAt < TimeSpan.FromHours(1))
                    return Domain.AsPerson(cachedUser);

                if (DateTimeOffset.UtcNow - cachedUser.CacheRefreshAttemptedAt < TimeSpan.FromMinutes(5))
                    return Domain.AsPerson(cachedUser);

                cachedUser.CacheRefreshAttemptedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            var whoami = await _weasylClient.WhoamiAsync();

            var weasylUser = await _weasylClient.GetUserAsync(whoami.login);

            if (cachedUser == null)
            {
                cachedUser = new User
                {
                    UserId = WEASYL_MIRROR_ACTOR
                };
                _context.Users.Add(cachedUser);
            }

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
                var key = await _keyProvider.GetPublicKeyAsync();

                var followers = await _context.Followers.ToListAsync();

                var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);
                foreach (var inbox in inboxes)
                {
                    _context.OutboundActivities.Add(new OutboundActivity
                    {
                        Id = Guid.NewGuid(),
                        Inbox = inbox.Key,
                        JsonBody = AP.SerializeWithContext(AP.PersonToUpdate(newUser, key)),
                        StoredAt = DateTimeOffset.UtcNow
                    });
                }
            }

            cachedUser.CacheRefreshSucceededAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return newUser;
        }
    }
}
