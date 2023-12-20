using Crowmask.Data;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.Remote
{
    public class OutboundActivityProcessor(CrowmaskDbContext context)
    {
        private async IAsyncEnumerable<OutboundActivity> GetOutboundActivities()
        {
            HashSet<Guid> retrieved = [];
            DateTimeOffset startAt = DateTimeOffset.MinValue;

            while (true)
            {
                var activities = await context.OutboundActivities
                    .Where(a => a.StoredAt >= startAt)
                    .Where(a => !a.Sent)
                    .OrderBy(a => a.StoredAt)
                    .Take(100)
                    .ToListAsync();

                foreach (var a in activities)
                {
                    if (retrieved.Contains(a.Id))
                        continue;

                    retrieved.Add(a.Id);

                    yield return a;

                    startAt = a.StoredAt;
                }

                if (activities.Count == 0)
                    break;
            }
        }

        public async Task ProcessOutboundActivities()
        {
            HashSet<string> inboxesToSkip = [];

            await foreach (var activity in GetOutboundActivities())
            {
                if (activity.DelayUntil > DateTimeOffset.UtcNow)
                    inboxesToSkip.Add(activity.Inbox);

                if (inboxesToSkip.Contains(activity.Inbox))
                    continue;

                try
                {
                    await Requests.SendAsync(activity);
                    activity.Sent = true;
                }
                catch (HttpRequestException)
                {
                    activity.DelayUntil = DateTimeOffset.UtcNow.AddHours(4);
                    inboxesToSkip.Add(activity.Inbox);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
