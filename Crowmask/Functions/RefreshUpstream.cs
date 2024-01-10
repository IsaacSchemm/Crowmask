using System;
using System.Threading.Tasks;
using Crowmask.DomainModeling;
using Crowmask.Interfaces;
using Crowmask.Remote;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class RefreshUpstream(ICrowmaskCache crowmaskCache, OutboundActivityProcessor outboundActivityProcessor, WeasylUserClient weasylUserClient)
    {
        /// <summary>
        /// Refreshes the cache for stale items that are among the following:
        /// <list type="bullet">
        /// <item>The most recent submission on Weasyl, plus any additional submissions within the past day</item>
        /// <item>The most recent journal on Weasyl, plus any additional journals within the past day</item>
        /// <item>Any cached posts in Crowmask that were posted to Weasyl within the past day</item>
        /// </list>
        /// This function handles new post discovery, updates, and deletions
        /// in the short term, and is responsible for sending outbound
        /// activities. Runs every ten minutes.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("ShortUpdate")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
        {
            DateTimeOffset yesterday = DateTimeOffset.UtcNow.AddDays(-1);

            await foreach (var submission in weasylUserClient.GetMyGallerySubmissionsAsync())
            {
                var cacheResult = await crowmaskCache.UpdateSubmissionAsync(submission.submitid);
                if (cacheResult is CacheResult.PostResult pr && pr.Post.first_upstream < yesterday)
                    break;
            }

            await foreach (int journalid in weasylUserClient.GetMyJournalIdsAsync())
            {
                var cacheResult = await crowmaskCache.UpdateJournalAsync(journalid);
                if (cacheResult is CacheResult.PostResult pr && pr.Post.first_upstream < yesterday)
                    break;
            }

            await foreach (var post in crowmaskCache.GetAllCachedPostsAsync())
            {
                if (post.first_upstream < yesterday)
                    break;

                if (post.identifier.IsSubmissionIdentifier)
                    await crowmaskCache.UpdateSubmissionAsync(post.identifier.submitid);
                if (post.identifier.IsJournalIdentifier)
                    await crowmaskCache.UpdateJournalAsync(post.identifier.journalid);
            }

            await outboundActivityProcessor.ProcessOutboundActivities();
        }
    }
}
