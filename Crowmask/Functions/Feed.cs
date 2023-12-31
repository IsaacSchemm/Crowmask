using Crowmask.Cache;
using Crowmask.Feed;
using Crowmask.Markdown;
using Crowmask.Merging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Feed(CrowmaskCache crowmaskCache, FeedBuilder feedBuilder)
    {
        [Function("Feed")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/feed")] HttpRequestData req)
        {
            var posts =
                await new[] {
                    crowmaskCache.GetCachedSubmissionsAsync(),
                    crowmaskCache.GetCachedJournalsAsync()
                }
                .MergeNewest(post => post.first_upstream)
                .Take(20)
                .ToListAsync();

            var person = await crowmaskCache.UpdateUserAsync();

            string rss = feedBuilder.ToRssFeed(person, posts);

            return await req.WriteCrowmaskResponseAsync(CrowmaskFormat.RSS, rss);
        }
    }
}
