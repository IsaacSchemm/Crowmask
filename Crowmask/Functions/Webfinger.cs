using Crowmask.Cache;
using Crowmask.DomainModeling;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class WebFinger(CrowmaskCache crowmaskCache, IHandleHost handleHost, IAdminActor adminActor, ICrowmaskHost host)
    {
        [Function("WebFinger")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/webfinger")] HttpRequestData req)
        {
            if (req.Query["resource"] is not string resource)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var person = await crowmaskCache.GetUser();

            string actor = $"https://{host.Hostname}/api/actor";

            string handle = $"acct:{person.preferredUsername}@{handleHost.Hostname}";

            if (resource == handle || resource == actor)
            {
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(new
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
                return resp;
            }
            else if (Uri.TryCreate(adminActor.Id, UriKind.Absolute, out Uri adminActorUri))
            {
                var redirectUri = new Uri(adminActorUri, $"/.well-known/webfinger?resource={Uri.EscapeDataString(resource)}");
                var resp = req.CreateResponse(HttpStatusCode.TemporaryRedirect);
                resp.Headers.Add("Location", redirectUri.AbsoluteUri);
                return resp;
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }
    }
}
