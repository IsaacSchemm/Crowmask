using System.Linq;
using System.Threading.Tasks;
using Crowmask.Library.Cache;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class RefreshCached(CrowmaskCache crowmaskCache)
    {
        /// <summary>
        /// Refreshes the cache for all cached posts that Crowmask indicates
        /// are stale. Runs every day at four minutes before midnight.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshCached")]
        public async Task Run([TimerTrigger("0 56 23 * * *")] TimerInfo myTimer)
        {
            var posts = crowmaskCache.GetCachedSubmissionsAsync()
                .Where(post => post.stale);

            await foreach (var post in posts)
            {
                await crowmaskCache.GetSubmissionAsync(post.submitid);
            }
        }
    }
}
