using System;
using System.Threading.Tasks;
using Crowmask.Cache;
using Crowmask.DomainModeling;
using Crowmask.Remote;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class RefreshUpstream(CrowmaskCache crowmaskCache, OutboundActivityProcessor outboundActivityProcessor, WeasylUserClient weasylUserClient)
    {
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
