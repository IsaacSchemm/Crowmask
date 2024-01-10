using Crowmask.Dependencies.Mapping;
using Crowmask.Interfaces;
using Crowmask.Library.Cache;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class WebFinger(ActivityStreamsIdMapper mapper, CrowmaskCache crowmaskCache, ICrowmaskHost crowmaskHost, IHandleHost handleHost, IAdminActor adminActor)
    {
        /// <summary>
        /// Points the user agent to the Crowmask actor ID, or redirects to the equivalent endpoint on the admin actor's server.
        /// </summary>
        /// <param name="req"></param>
        /// <returns>A WebFinger response</returns>
        [Function("WebFinger")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/webfinger")] HttpRequestData req)
        {
            if (req.Query["resource"] is not string resource)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var person = await crowmaskCache.GetUserAsync();

            string handle = $"acct:{person.preferredUsername}@{handleHost.Hostname}";
            string alternate = $"acct:{person.preferredUsername}@{crowmaskHost.Hostname}";

            if (resource == handle || resource == mapper.ActorId || resource == alternate)
            {
                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(new
                {
                    subject = handle,
                    aliases = new[] { alternate, mapper.ActorId }.Except([handle]),
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
                            href = mapper.ActorId
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
