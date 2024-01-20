using Crowmask.Data;
using Crowmask.Interfaces;
using Crowmask.HighLevel.Remote;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.HighLevel
{
    public class RemoteInboxLocator(CrowmaskDbContext context, IApplicationInformation appInfo, Requester requester)
    {
        /// <summary>
        /// Gets the URL of the admin actor's inbox.
        /// </summary>
        /// <returns>The inbox URL</returns>
        public async Task<string> GetAdminActorInboxAsync()
        {
            var follower = await context.Followers
                .Where(f => f.ActorId == appInfo.AdminActorId)
                .Select(f => new { f.Inbox })
                .FirstOrDefaultAsync();

            if (follower != null)
                return follower.Inbox;

            var adminActorDetails = await requester.FetchActorAsync(appInfo.AdminActorId);
            return adminActorDetails.Inbox;
        }

        /// <summary>
        /// Gets the URLs of known inboxes.
        /// </summary>
        /// <param name="followersOnly">Whether to limit the set to only followers' servers. If false, Crowmask will include all known inboxes.</param>
        /// <returns>A set of inbox URLs</returns>
        public async Task<IReadOnlySet<string>> GetDistinctInboxesAsync(bool followersOnly = false)
        {
            HashSet<string> inboxes = [];

            // Go through follower inboxes first - prefer shared inbox if present
            await foreach (var follower in context.Followers.AsAsyncEnumerable())
                inboxes.Add(follower.SharedInbox ?? follower.Inbox);

            // Then include all other known inboxes, if enabled
            if (!followersOnly)
                await foreach (var known in context.KnownInboxes.AsAsyncEnumerable())
                    inboxes.Add(known.Inbox);

            return inboxes;
        }
    }
}
