using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crowmask.Data;
using Crowmask.Library;
using JsonLD.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.Functions
{
    public class RefreshCached(SubmissionCache cache, CrowmaskDbContext context)
    {
        /// <summary>
        /// Refreshes the cache for up to 100 cached posts that Crowmask says
        /// are stale. Runs every day at four minutes before midnight.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshCached")]
        public async Task Run([TimerTrigger("0 56 23 * * *")] TimerInfo myTimer)
        {
            List<int> ids = [];

            var sequence = context.Submissions
                .OrderByDescending(s => s.PostedAt)
                .AsAsyncEnumerable();

            await foreach (var submission in sequence)
            {
                if (submission.Stale)
                    ids.Add(submission.SubmitId);

                if (ids.Count >= 100)
                    break;
            }

            foreach (int submitid in ids)
                await cache.RefreshSubmissionAsync(submitid);
        }
    }
}
