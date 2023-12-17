using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Crowmask.Data;
using Crowmask.ActivityPub;

namespace Crowmask.Functions
{
    public class Delete(CrowmaskDbContext context)
    {
        [FunctionName("Delete")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var activity = AP.AsActivity(
                Domain.AsDelete(7),
                Recipient.NewActorRecipient("https://microblog.lakora.us"));

            //await Requests.SendAsync(AP.ACTOR, "https://microblog.lakora.us", activity);

            return new OkObjectResult("test");
        }
    }
}
