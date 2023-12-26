using System.Threading.Tasks;
using Crowmask.Cache;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Crowmask.Functions
{
    public class ActorUpdate(CrowmaskCache crowmaskCache)
    {
        [FunctionName("ActorUpdate")]
        public async Task Run([TimerTrigger("15 49 * * * *")]TimerInfo myTimer, ILogger log)
        {
            await crowmaskCache.GetUser();
        }
    }
}
