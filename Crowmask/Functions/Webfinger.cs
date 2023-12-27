using Crowmask.ActivityPub;
using Crowmask.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class WebFinger(CrowmaskCache crowmaskCache, IHandleHost handleHost, IAdminActor adminActor, ICrowmaskHost host)
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
                    Content = "\"resource\" parameter is missing or invalid",
                    ContentType = "text/plain",
                    StatusCode = 400
                };
            }

            string resource = req.Query["resource"].Single();

            var person = await crowmaskCache.GetUser();

            string actor = $"https://{host.Hostname}/api/actor";

            string handle = $"acct:{person.preferredUsername}@{handleHost.Hostname}";

            if (resource == handle || resource == actor)
            {
                return new JsonResult(new
                {
                    subject = handle,
                    aliases = new[] { actor },
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
                            href = actor
                        }
                    }
                });
            }
            else if (Uri.TryCreate(adminActor.Id, UriKind.Absolute, out Uri adminActorUri))
            {
                var redirectUri = new Uri(adminActorUri, $"/.well-known/webfinger?resource={Uri.EscapeDataString(resource)}");
                return new RedirectResult(redirectUri.AbsoluteUri);
            }
            else
            {
                return new NotFoundResult();
            }
        }
    }
}
