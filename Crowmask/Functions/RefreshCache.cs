using Crowmask.Library;
using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class RefreshCache(SubmissionCache cache)
    {
        /// <summary>
        /// Refreshes all stale posts. Runs every day at 12:00.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshCache")]
        public async Task Run([TimerTrigger("0 0 12 * * *")] TimerInfo myTimer)
        {
            await foreach (var post in cache.GetCachedSubmissionsAsync())
                if (post.stale)
                    await cache.RefreshSubmissionAsync(post.submitid);
        }
    }
}
