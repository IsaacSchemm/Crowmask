using System;
using System.Linq;
using System.Threading.Tasks;
using Crowmask.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.Functions
{
    public class OutboundActivityCleanup(CrowmaskDbContext context)
    {
        [Function("OutboundActivityCleanup")]
        public async Task Run([TimerTrigger("0 2 * * * *")] TimerInfo myTimer)
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromDays(7);

            while (true)
            {
                var activities = await context.OutboundActivities
                    .Where(a => a.StoredAt < cutoff)
                    .OrderBy(a => a.StoredAt)
                    .Take(100)
                    .ToListAsync();

                if (activities.Count == 0)
                    break;

                context.OutboundActivities.RemoveRange(activities);
                await context.SaveChangesAsync();
            }
        }
    }
}
