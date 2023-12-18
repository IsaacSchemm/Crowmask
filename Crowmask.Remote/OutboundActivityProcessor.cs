using Crowmask.Data;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.Remote
{
    public class OutboundActivityProcessor(CrowmaskDbContext context)
    {
        public async Task ProcessOutboundActivities(int? userid = null, int? submitid = null)
        {
            var activities = await context.OutboundActivities
                .Where(a => !a.Sent)
                .OrderBy(a => a.Failures)
                .ThenBy(a => a.PublishedAt)
                .Take(500)
                .ToListAsync();

            foreach (var activity in activities)
            {
                try
                {
                    await Requests.SendAsync(activity);
                    activity.Sent = true;
                }
                catch (HttpRequestException)
                {
                    activity.Failures++;
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
