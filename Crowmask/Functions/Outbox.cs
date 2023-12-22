using Crowmask.ActivityPub;
using Crowmask.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Outbox(CrowmaskCache crowmaskCache, Translator translator)
    {
        [FunctionName("Outbox")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox")] HttpRequest req,
            ILogger log)
        {
            var recent = await crowmaskCache
                .GetSubmissionsAsync(max: 20)
                .ToListAsync();

            var outbox = translator.AsOutbox(recent);

            string json = AP.SerializeWithContext(outbox);

            return new ContentResult
            {
                Content = json,
                ContentType = "application/json"
            };
        }
    }
}
