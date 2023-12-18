﻿using CrosspostSharp3.Weasyl;
using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.Weasyl;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Crowmask.Cache
{
    public class CrowmaskCache
    {
        private readonly CrowmaskDbContext _context;
        public readonly IPublicKey _publicKey;
        private readonly WeasylClient _weasylClient;

        public CrowmaskCache(CrowmaskDbContext context, IPublicKey publicKey, IWeasylApiKeyProvider apiKeyProvider)
        {
            _context = context;
            _publicKey = publicKey;
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

        public async Task<Domain.Person> GetUser()
        {
            var whoami = await _weasylClient.WhoamiAsync();

            var weasylUser = await _weasylClient.GetUserAsync(whoami.login);

            var cachedUser = await _context.Users
                .Include(u => u.Avatars)
                .Include(u => u.Links)
                .Where(u => u.UserId == whoami.userid)
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
            else
            {
                cachedUser = new User
                {
                    UserId = whoami.userid
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
                var followers = await _context.Followers.ToListAsync();

                var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);
                foreach (var inbox in inboxes)
                {
                    _context.OutboundActivities.Add(new OutboundActivity
                    {
                        Inbox = inbox.Key,
                        JsonBody = AP.SerializeWithContext(AP.PersonToUpdate(newUser, _publicKey)),
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
