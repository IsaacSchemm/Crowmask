using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class RefreshUpstreamNew(SubmissionCache cache, WeasylClient weasylClient)
    {
        /// <summary>
        /// Refreshes all submissions in the user's Weasyl gallery that are newer than the most recent cached submission.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshUpstreamNew")]
        public async Task Run([TimerTrigger("0 15 16 * * *")] TimerInfo myTimer)
        {
            await foreach (var submission in weasylClient.GetMyGallerySubmissionsAsync())
            {
                var cached = await cache.GetCachedSubmissionAsync(submission.submitid);
                if (cached.IsPostResult)
                    break;

                await cache.RefreshSubmissionAsync(submission.submitid);
            }
        }
    }
}
