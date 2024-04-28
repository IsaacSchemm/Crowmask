using System;
using System.Threading.Tasks;
using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class RefreshRecent(SubmissionCache cache, WeasylClient weasylClient)
    {
        /// <summary>
        /// Refreshes posts that were originally posted within the past day.
        /// Runs every ten minutes.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshRecent")]
        public async Task Run([TimerTrigger("* */10 * * * *")] TimerInfo myTimer)
        {
            DateTimeOffset yesterday = DateTimeOffset.UtcNow.AddDays(-1);

            await foreach (var submission in weasylClient.GetMyGallerySubmissionsAsync())
            {
                await cache.RefreshSubmissionAsync(submission.submitid);
                if (submission.posted_at < yesterday)
                    break;
            }

            await foreach (var post in cache.GetCachedSubmissionsAsync(since: yesterday))
                if (post.stale)
                    await cache.RefreshPostAsync(post.id);

            await foreach (var post in cache.GetCachedJournalsAsync(since: yesterday))
                if (post.stale)
                    await cache.RefreshPostAsync(post.id);
        }
    }
}
