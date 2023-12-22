using Crowmask.ActivityPub;
using Crowmask.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class WebFinger(CrowmaskCache crowmaskCache, ICrowmaskHost host)
    {
        [FunctionName("WebFinger")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/webfinger")] HttpRequest req,
            ILogger log)
        {
            if (req.Query["resource"].Count != 1)
            {
                return new ContentResult
                {
                    Content = "\"resource\" parameter is missing",
                    ContentType = "text/plain",
                    StatusCode = 400
                };
            }

            string resource = req.Query["resource"].Single();

            var person = await crowmaskCache.GetUser();

            string primaryActor = $"https://{host.Hostname}/api/actor";

            if (resource == $"acct:{person.preferredUsername}@{host.Hostname}" || resource == $"https://{host.Hostname}/api/actor")
            {
                return new JsonResult(new
                {
                    subject = resource,
                    aliases = new[] { primaryActor },
                    links = new[]
                    {
                        new
                        {
                            rel = "http://webfinger.net/rel/profile-page",
                            type = "text/html",
                            href = person.url
                        },
                        new
                        {
                            rel = "self",
                            type = "application/activity+json",
                            href = primaryActor
                        }
                    }
                });
            }
            else
            {
                return new NotFoundResult();
            }
        }
    }
}
