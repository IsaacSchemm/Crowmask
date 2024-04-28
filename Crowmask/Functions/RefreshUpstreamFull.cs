using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class RefreshUpstreamFull(SubmissionCache cache, WeasylClient weasylClient)
    {
        /// <summary>
        /// Refreshes all submissions in the user's Weasyl gallery that are missing or stale in the cache.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshUpstreamFull")]
        public async Task Run([TimerTrigger("0 0 17 5 * *")] TimerInfo myTimer)
        {
            await foreach (var submission in weasylClient.GetMyGallerySubmissionsAsync())
                await cache.RefreshSubmissionAsync(submission.submitid);
        }
    }
}
