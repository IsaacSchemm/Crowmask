using Crowmask.Interfaces;
using Crowmask.Library.Cache;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class NodeInfo(CrowmaskCache crowmaskCache, IHandleHost handleHost, IHandleName handleName)
    {
        /// <summary>
        /// Returns a NodeInfo 2.2 response with information about the user and about Crowmask.
        /// </summary>
        /// <param name="req"></param>
        /// <returns>A NodeInfo response</returns>
        [Function("NodeInfo")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/nodeinfo")] HttpRequestData req)
        {
            var user = await crowmaskCache.GetUserAsync();

            int postCount = await crowmaskCache.GetCachedSubmissionCountAsync();

            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", $"application/json; charset=utf-8");
            await resp.WriteStringAsync(JsonSerializer.Serialize(new
            {
                version = "2.2",
                instance = new
                {
                    name = $"@{handleName.PreferredUsername}@{handleHost.Hostname}",
                    description = $"An ActivityPub mirror of artwork posted to Weasyl by {user.upstreamUsername}"
                },
                software = new
                {
                    name = "crowmask",
                    version = "1.0.0",
                    repository = "https://github.com/IsaacSchemm/Crowmask",
                    homepage = "https://github.com/IsaacSchemm/Crowmask"
                },
                protocols = new[]
                {
                    "activitypub"
                },
                services = new
                {
                    inbound = Array.Empty<object>(),
                    outbound = new[]
                    {
                        "atom1.0",
                        "rss2.0"
                    }
                },
                openRegistrations = false,
                usage = new
                {
                    users = new
                    {
                        total = 1
                    },
                    localPosts = postCount
                },
                metadata = new { }
            }), Encoding.UTF8);
            return resp;
        }
    }
}
