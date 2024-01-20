using Crowmask.Data;
using Crowmask.Formats;
using Crowmask.Interfaces;
using Crowmask.LowLevel;

namespace Crowmask.Library
{
    /// <summary>
    /// Accesses and updates cached user information in the Crowmask database.
    /// </summary>
    public class UserCache(
        ActivityPubTranslator translator,
        CrowmaskDbContext context,
        ICrowmaskKeyProvider keyProvider,
        RemoteInboxLocator inboxLocator,
        WeasylClient weasylClient)
    {
        /// <summary>
        /// Gets cached user profile information for the user who issued the
        /// Weasyl API key. Crowmask will only try to get new information from
        /// Weasyl if it cannot find the user in its own database.
        /// </summary>
        /// <returns></returns>
        public async Task<Person> GetUserAsync()
        {
            var cachedUser = await context.GetUserAsync();
            return cachedUser != null
                ? Domain.AsPerson(cachedUser)
                : await UpdateUserAsync();
        }

        /// <summary>
        /// Attempts to refresh user profile information from Weasyl. Any
        /// changes will generate new ActivityPub messages.
        /// </summary>
        /// <returns>A Person object</returns>
        public async Task<Person> UpdateUserAsync()
        {
            var cachedUser = await context.GetUserAsync();

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

            cachedUser.Links = weasylUser.user_info.user_links
                .SelectMany(pair => pair.Value
                    .Select(val => new UserLink
                    {
                        Id = Guid.NewGuid(),
                        Site = pair.Key,
                        UsernameOrUrl = val
                    }))
                .ToList();

            var newUser = Domain.AsPerson(cachedUser);

            if (!oldUser.Equals(newUser))
            {
                var key = await keyProvider.GetPublicKeyAsync();

                foreach (string inbox in await inboxLocator.GetDistinctInboxesAsync())
                {
                    context.OutboundActivities.Add(new OutboundActivity
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

            await context.SaveChangesAsync();

            return newUser;
        }
    }
}
