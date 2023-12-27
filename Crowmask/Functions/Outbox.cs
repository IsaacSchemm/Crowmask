using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Weasyl;
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
    public class Outbox(CrowmaskCache crowmaskCache, Translator translator, WeasylClient weasylClient)
    {
        [FunctionName("Outbox")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox")] HttpRequest req,
            ILogger log)
        {
            var whoami = await weasylClient.WhoamiAsync();

            var recent = await weasylClient.GetUserGallerySubmissionsAsync(whoami.login)
                .Take(20)
                .SelectAwait(async x => await crowmaskCache.GetSubmission(x.submitid))
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
