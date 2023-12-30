using System;
using System.Threading.Tasks;
using Crowmask.Cache;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class LongUpdate(CrowmaskCache crowmaskCache, Synchronizer synchronizer)
    {
        [Function("LongUpdate")]
        public async Task Run([TimerTrigger("0 56 23 * * *")] TimerInfo myTimer)
        {
            await crowmaskCache.GetUserAsync();

            await synchronizer.SynchronizeAsync(cutoff: DateTimeOffset.MinValue);
        }
    }
}
