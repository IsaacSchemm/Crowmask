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
using Crowmask.Cache;

namespace Crowmask.Functions
{
    public class Update(CrowmaskCache crowmaskCache, IPublicKey key)
    {
        [FunctionName("Update")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var person = await crowmaskCache.GetUser();

            var activity = AP.PersonToUpdate(person, key);

            await Requests.SendAsync(AP.ACTOR, "https://microblog.lakora.us", activity);

            return new OkObjectResult("test");
        }
    }
}
