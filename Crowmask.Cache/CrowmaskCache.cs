using CrosspostSharp3.Weasyl;
using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.Weasyl;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Crowmask.Cache
{
    public interface ICrowmaskCache
    {
        Task<Domain.Note> GetSubmission(int submitid);
        Task<Domain.Person> GetUser();
    }

    public class CrowmaskCache : ICrowmaskCache
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

        public Task<Domain.Note> GetSubmission(int submitid)
        {
            throw new NotImplementedException();
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

            if (cachedUser == null)
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

            //if (!oldUser.Equals(newUser))
            //{
            //    var followers = await _context.Followers.ToListAsync();

            //    var inboxes = followers.GroupBy(f => f.SharedInbox ?? f.Inbox);
            //    foreach (var inbox in inboxes)
            //    {
            //        _context.OutboundActivities.Add(new OutboundActivity
            //        {
            //            Inbox = inbox.Key,
            //            JsonBody = JsonSerializer.Serialize(AP.PersonToUpdate(newUser, _publicKey)),
            //            PublishedAt = DateTimeOffset.UtcNow
            //        });
            //    }
            //}

            cachedUser.CacheRefreshSucceededAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return newUser;
        }
    }
}
