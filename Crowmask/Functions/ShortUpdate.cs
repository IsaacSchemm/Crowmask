using System;
using System.Threading.Tasks;
using Crowmask.Remote;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Crowmask.Functions
{
    public class ShortUpdate(OutboundActivityProcessor outboundActivityProcessor, Synchronizer synchronizer)
    {
        [FunctionName("ShortUpdate")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            await synchronizer.SynchronizeAsync(cutoff: DateTimeOffset.UtcNow - TimeSpan.FromDays(60));

            await outboundActivityProcessor.ProcessOutboundActivities();
        }
    }
}
