using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class RefreshStale(SubmissionCache cache)
    {
        /// <summary>
        /// Refreshes all stale posts.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshStale")]
        public async Task Run([TimerTrigger("0 0 12 * * *")] TimerInfo myTimer)
        {
            await foreach (var post in cache.GetCachedSubmissionsAsync())
                if (post.stale)
                    await cache.RefreshPostAsync(post.id);

            await foreach (var post in cache.GetCachedJournalsAsync())
                if (post.stale)
                    await cache.RefreshPostAsync(post.id);
        }
    }
}
