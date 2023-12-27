using System;
using System.Linq;
using System.Threading.Tasks;
using Crowmask.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crowmask.Functions
{
    public class OutboundActivityCleanup(CrowmaskDbContext context)
    {
        [FunctionName("OutboundActivityCleanup")]
        public async Task Run([TimerTrigger("0 2 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromDays(7);

            while (true)
            {
                var activities = await context.OutboundActivities
                    .Where(a => a.StoredAt < cutoff)
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
