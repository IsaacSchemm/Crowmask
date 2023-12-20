using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Remote;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class WebFinger(CrowmaskCache crowmaskCache)
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

            if (resource == $"acct:{person.preferredUsername}@{AP.HOST}" || resource == AP.ACTOR)
            {
                return new JsonResult(new
                {
                    subject = resource,
                    aliases = new[] { AP.ACTOR },
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
                            href = AP.ACTOR
                        }
                    }
                });
            }
            else if (resource == $"acct:crowmask@{AP.HOST}" || resource == $"https://{AP.HOST}/api/secondary-actor")
            {
                throw new NotImplementedException();
            }
            else
            {
                return new NotFoundResult();
            }
        }
    }
}
