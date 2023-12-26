using System;
using System.Threading.Tasks;
using Crowmask.Cache;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Crowmask.Functions
{
    public class LongUpdate(CrowmaskCache crowmaskCache, Synchronizer synchronizer)
    {
        [FunctionName("LongUpdate")]
        public async Task Run([TimerTrigger("0 56 23 * * *")] TimerInfo myTimer, ILogger log)
        {
            await crowmaskCache.GetUser();

            await synchronizer.SynchronizeAsync(cutoff: DateTimeOffset.MinValue);
        }
    }
}
