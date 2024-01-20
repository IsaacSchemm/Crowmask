using System.Threading.Tasks;
using Crowmask.HighLevel.Remote;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class SendOutbound(OutboundActivityProcessor outboundActivityProcessor)
    {
        /// <summary>
        /// Sends pending outbound activities. Runs every minute.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("SendOutbound")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            await outboundActivityProcessor.ProcessOutboundActivities();
        }
    }
}
