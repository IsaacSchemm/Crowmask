using System;
using System.Threading.Tasks;
using Crowmask.Dependencies.Weasyl;
using Crowmask.DomainModeling;
using Crowmask.Library.Cache;
using Crowmask.Library.Remote;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class RefreshUpstream(CrowmaskCache crowmaskCache, OutboundActivityProcessor outboundActivityProcessor, WeasylUserClient weasylUserClient)
    {
        /// <summary>
        /// Refreshes the cache for stale items that are among the following:
        /// <list type="bullet">
        /// <item>The most recent submission on Weasyl, plus any additional submissions within the past day</item>
        /// <item>The most recent journal on Weasyl, plus any additional journals within the past day</item>
        /// <item>The user profile</item>
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
                var cacheResult = await crowmaskCache.GetSubmissionAsync(submission.submitid);
                if (cacheResult is CacheResult.PostResult pr && pr.Post.first_upstream < yesterday)
                    break;
            }

            await foreach (int journalid in weasylUserClient.GetMyJournalIdsAsync())
            {
                var cacheResult = await crowmaskCache.GetJournalAsync(journalid);
                if (cacheResult is CacheResult.PostResult pr && pr.Post.first_upstream < yesterday)
                    break;
            }

            await crowmaskCache.GetUserAsync();

            await foreach (var post in crowmaskCache.GetAllCachedPostsAsync())
            {
                if (post.first_upstream < yesterday)
                    break;

                if (post.identifier.IsSubmissionIdentifier)
                    await crowmaskCache.GetSubmissionAsync(post.identifier.submitid);
                if (post.identifier.IsJournalIdentifier)
                    await crowmaskCache.GetJournalAsync(post.identifier.journalid);
            }

            await outboundActivityProcessor.ProcessOutboundActivities();
        }
    }
}
