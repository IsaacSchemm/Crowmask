using Crowmask.Cache;
using Crowmask.DomainModeling;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class NodeInfo(CrowmaskCache crowmaskCache, IHandleHost handleHost)
    {
        [Function("NodeInfo")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/nodeinfo")] HttpRequestData req)
        {
            var user = await crowmaskCache.GetUserAsync();

            int postCount = await AsyncEnumerable.Empty<Post>()
                .Concat(crowmaskCache.GetCachedSubmissionsAsync())
                .Concat(crowmaskCache.GetCachedJournalsAsync())
                .CountAsync();

            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", $"application/json; charset=utf-8");
            await resp.WriteStringAsync(JsonSerializer.Serialize(new
            {
                version = "2.2",
                instance = new
                {
                    name = $"@{user.preferredUsername}@{handleHost.Hostname}",
                    description = $"An ActivityPub mirror of artwork and journals posted to Weasyl by {user.preferredUsername}"
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
