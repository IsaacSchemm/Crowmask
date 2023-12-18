using System.Threading.Tasks;
using Crowmask.Remote;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Crowmask.Functions
{
    public class OutboundActivitySend(OutboundActivityProcessor outboundActivityProcessor)
    {
        [FunctionName("OutboundActivitySend")]
        public async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            await outboundActivityProcessor.ProcessOutboundActivities();
        }
    }
}
