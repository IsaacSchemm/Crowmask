using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Crowmask.ActivityPub;
using Crowmask.Cache;

namespace Crowmask.Functions
{
    public class Submissions(CrowmaskCache cache, Translator translator)
    {
        [FunctionName("Submissions")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "api/submissions/{submitid}")] HttpRequest req,
            int submitid,
            ILogger log)
        {
            var submission = await cache.GetSubmission(submitid);

            return submission == null
                ? new NotFoundResult()
                : new ContentResult
                {
                    Content = AP.SerializeWithContext(translator.AsObject(submission)),
                    ContentType = "application/json"
                };
        }
    }
}
