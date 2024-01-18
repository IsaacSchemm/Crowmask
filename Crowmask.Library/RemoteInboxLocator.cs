using Crowmask.Data;
using Crowmask.Interfaces;
using Crowmask.Library.Remote;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.Library
{
    public class RemoteInboxLocator(CrowmaskDbContext context, IAdminActor adminActor, Requester requester)
    {
        private readonly Lazy<Task<string>> _inboxTask = new(async () =>
        {
            var follower = await context.Followers
                .Where(f => f.ActorId == adminActor.Id)
                .Select(f => new { f.Inbox })
                .FirstOrDefaultAsync();

            if (follower != null)
                return follower.Inbox;

            var adminActorDetails = await requester.FetchActorAsync(adminActor.Id);
            return adminActorDetails.Inbox;
        });

        public Task<string> GetAdminActorInboxAsync() => _inboxTask.Value;
    }
}
