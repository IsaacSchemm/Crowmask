using Crowmask.Data;
using Crowmask.HighLevel.ATProto;
using Crowmask.LowLevel;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.HighLevel.Notifications
{
    public class NotificationCollector(
        ApplicationInformation appInfo,
        IDbContextFactory<CrowmaskDbContext> contextFactory,
        IHttpClientFactory httpClientFactory)
    {
        private async IAsyncEnumerable<Notification> GetInteractionsAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var query = context.Interactions
                .OrderByDescending(i => i.AddedAt)
                .AsAsyncEnumerable();

            await foreach (var i in query)
            {
                yield return new Notification(
                    Category: "ActivityStreams activity",
                    Action: i.ActivityType.Replace("https://www.w3.org/ns/activitystreams#", ""),
                    User: i.ActorId,
                    Context: i.TargetId,
                    Timestamp: i.AddedAt);
            }
        }

        private async IAsyncEnumerable<Notification> GetMentionsAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var query = context.Mentions
                .OrderByDescending(m => m.AddedAt)
                .AsAsyncEnumerable();

            await foreach (var m in query)
            {
                yield return new Notification(
                    Category: "ActivityPub mention",
                    Action: "Mention / Reply",
                    User: m.ActorId,
                    Context: m.ObjectId,
                    Timestamp: m.AddedAt);
            }
        }

        private async IAsyncEnumerable<Notification> GetBlueskyNotificationsAsync(BlueskyAccountConfiguration account)
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var session = await context.BlueskySessions
                .Where(s => s.DID == account.DID)
                .SingleOrDefaultAsync();

            if (session == null)
                yield break;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var wrapper = new TokenWrapper(context, session);

            await foreach (var n in Crowmask.ATProto.Notifications.ListAllNotificationsAsync(client, wrapper))
            {
                yield return new Notification(
                    Category: "Bluesky notification",
                    Action: n.reason,
                    User: n.author.handle,
                    Context: n.reasonSubject,
                    Timestamp: n.indexedAt);
            }
        }

        private IEnumerable<IAsyncEnumerable<Notification>> GetAllNotificationSequences()
        {
            yield return GetInteractionsAsync();
            yield return GetMentionsAsync();
            foreach (var account in appInfo.BlueskyBotAccounts)
                yield return GetBlueskyNotificationsAsync(account);
        }

        public IAsyncEnumerable<Notification> GetAllNotificationsAsync()
        {
            return GetAllNotificationSequences()
                .MergeNewest(obj => obj.Timestamp);
        }
    }
}
