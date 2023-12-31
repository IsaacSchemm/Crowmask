using System.Linq;
using System.Threading.Tasks;
using Crowmask.Cache;
using Crowmask.DomainModeling;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class RefreshCached(CrowmaskCache crowmaskCache)
    {
        [Function("RefreshCached")]
        public async Task Run([TimerTrigger("0 56 23 * * *")] TimerInfo myTimer)
        {
            await crowmaskCache.UpdateUserAsync();

            var posts = AsyncEnumerable.Empty<Post>()
                .Concat(crowmaskCache.GetCachedSubmissionsAsync())
                .Concat(crowmaskCache.GetCachedJournalsAsync())
                .Where(post => post.stale);

            await foreach (var post in posts)
            {
                if (post.upstream_type is UpstreamType.UpstreamSubmission submission)
                {
                    await crowmaskCache.UpdateSubmissionAsync(submission.submitid);
                }
                else if (post.upstream_type is UpstreamType.UpstreamJournal journal)
                {
                    await crowmaskCache.UpdateJournalAsync(journal.journalid);
                }
            }
        }
    }
}
