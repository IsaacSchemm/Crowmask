using Crowmask.Data;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.Remote
{
    public class OutboundActivityProcessor(CrowmaskDbContext context)
    {
        private async IAsyncEnumerable<OutboundActivity> GetOutboundActivities()
        {
            long minId = 0;

            while (true)
            {
                var activities = await context.OutboundActivities
                    .Where(a => a.Id >= minId)
                    .Where(a => !a.Sent)
                    .OrderBy(a => a.Id)
                    .Take(100)
                    .ToListAsync();

                foreach (var a in activities)
                    yield return a;

                if (activities.Count == 0)
                    break;

                minId = activities
                    .Select(a => a.Id)
                    .Max() + 1;
            }
        }

        public async Task ProcessOutboundActivities()
        {
            HashSet<string> inboxesToSkip = new();

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
