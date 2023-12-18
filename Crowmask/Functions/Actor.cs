using Crowmask.ActivityPub;
using Crowmask.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(CrowmaskCache crowmaskCache, IPublicKey key)
    {
        [FunctionName("Actor")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var person = await crowmaskCache.GetUser();

                string json = AP.SerializeWithContext(AP.PersonToObject(person, key));

                return new JsonResult(json);
            }
            catch (Exception ex) when (DateTime.UtcNow < new DateTime(2023, 12, 19))
            {
                return new ContentResult
                {
                    Content = ex.ToString(),
                    ContentType = "text/plain"
                };
            }
        }
    }
}
