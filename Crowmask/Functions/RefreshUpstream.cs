using Crowmask.Library;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class RefreshUpstream(SubmissionCache cache, WeasylClient weasylClient)
    {
        /// <summary>
        /// Refreshes all posts in the user's Weasyl gallery that are missing
        /// or stale in the cache. Runs every month on the 1st at 17:00.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshUpstream")]
        public async Task Run([TimerTrigger("0 0 17 1 * *")] TimerInfo myTimer)
        {
            await foreach (var submission in weasylClient.GetMyGallerySubmissionsAsync())
                await cache.RefreshSubmissionAsync(submission.submitid);
        }
    }
}
