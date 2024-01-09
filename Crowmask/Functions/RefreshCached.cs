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

            var posts = crowmaskCache.GetAllCachedPostsAsync()
                .Where(post => post.stale);

            await foreach (var post in posts)
            {
                if (post.identifier.IsSubmissionIdentifier)
                    await crowmaskCache.UpdateSubmissionAsync(post.identifier.submitid);
                else if (post.identifier.IsJournalIdentifier)
                    await crowmaskCache.UpdateJournalAsync(post.identifier.journalid);
            }
        }
    }
}
