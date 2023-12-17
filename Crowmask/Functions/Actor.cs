using Crowmask.ActivityPub;
using Crowmask.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(CrowmaskCache crowmaskCache, IPublicKey key)
    {
        [FunctionName("Actor")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "api/actor")] HttpRequest req,
            ILogger log)
        {
            var person = await crowmaskCache.GetUser();

            return new JsonResult(AP.PersonToObject(person, key));
        }
    }
}
