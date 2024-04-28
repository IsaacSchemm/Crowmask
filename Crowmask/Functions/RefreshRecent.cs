using System;
using System.Threading.Tasks;
using Crowmask.HighLevel;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class RefreshRecent(SubmissionCache cache)
    {
        /// <summary>
        /// Refreshes cached posts with an upstream post date within the last 30 minutes.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshRecent")]
        public async Task Run([TimerTrigger("* */5 * * * *")] TimerInfo myTimer)
        {
            DateTimeOffset old = DateTimeOffset.UtcNow.AddHours(-1);

            await foreach (var post in cache.GetCachedSubmissionsAsync(since: old))
                await cache.RefreshPostAsync(post.id);

            await foreach (var post in cache.GetCachedJournalsAsync(since: old))
                await cache.RefreshPostAsync(post.id);
        }
    }
}
