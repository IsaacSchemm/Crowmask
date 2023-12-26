using Crowmask.ActivityPub;
using Crowmask.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Creations(CrowmaskCache cache, Translator translator)
    {
        [FunctionName("Creations")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/creations/{submitid}")] HttpRequest req,
            int submitid,
            ILogger log)
        {
            var submission = await cache.GetSubmission(submitid);

            return submission == null
                ? new NotFoundResult()
                : new ContentResult
                {
                    Content = AP.SerializeWithContext(translator.ObjectToCreate(submission)),
                    ContentType = "application/activity+json"
                };
        }
    }
}
