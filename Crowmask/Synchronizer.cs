using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.Weasyl;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask
{
    public class Synchronizer(CrowmaskCache crowmaskCache, CrowmaskDbContext context, WeasylUserClient weasylUserClient)
    {
        public async Task SynchronizeAsync(DateTimeOffset cutoff)
        {
            await foreach (var submission in weasylUserClient.GetMyGallerySubmissionsAsync())
            {
                if (submission.posted_at < cutoff)
                    break;

                await crowmaskCache.GetSubmissionAsync(submission.submitid);
            }

            var cachedSubmissions = await context.Submissions
                .Where(s => s.PostedAt >= cutoff)
                .ToListAsync();

            foreach (var submission in cachedSubmissions)
            {
                if (submission.Stale)
                    await crowmaskCache.GetSubmissionAsync(submission.SubmitId);
            }

            await foreach (int journalId in weasylUserClient.GetMyJournalIdsAsync())
            {
                var journal = await crowmaskCache.GetJournalAsync(journalId);

                if (journal.first_upstream < cutoff)
                    break;
            }

            var cachedJournals = await context.Journals
                .Where(j => j.PostedAt >= cutoff)
                .ToListAsync();

            foreach (var journal in cachedJournals)
            {
                if (journal.Stale)
                    await crowmaskCache.GetJournalAsync(journal.JournalId);
            }
        }
    }
}
