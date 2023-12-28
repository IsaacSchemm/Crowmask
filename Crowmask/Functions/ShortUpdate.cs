using System;
using System.Threading.Tasks;
using Crowmask.Remote;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class ShortUpdate(OutboundActivityProcessor outboundActivityProcessor, Synchronizer synchronizer)
    {
        [Function("ShortUpdate")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            await synchronizer.SynchronizeAsync(cutoff: DateTimeOffset.UtcNow - TimeSpan.FromDays(60));

            await outboundActivityProcessor.ProcessOutboundActivities();
        }
    }
}
