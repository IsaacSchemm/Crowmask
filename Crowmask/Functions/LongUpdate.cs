using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Crowmask.Functions
{
    public class LongUpdate(Synchronizer synchronizer)
    {
        [FunctionName("LongUpdate")]
        public async Task Run([TimerTrigger("45 46 * * * *")] TimerInfo myTimer, ILogger log)
        {
            await synchronizer.SynchronizeAsync(cutoff: DateTimeOffset.MinValue);
        }
    }
}
