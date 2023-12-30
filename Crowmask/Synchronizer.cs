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
            // Update existing submissions
            var cachedSubmissions = await context.Submissions
                .Where(s => s.PostedAt >= cutoff)
                .ToListAsync();

            foreach (var submission in cachedSubmissions)
            {
                if (submission.Stale)
                    await crowmaskCache.GetSubmissionAsync(submission.SubmitId);
            }

            // Find ID of newest known submission
            var newestKnownSubmission =
                cachedSubmissions
                    .OrderByDescending(s => s.SubmitId)
                    .Select(s => new { s.SubmitId })
                    .FirstOrDefault()
                ?? await context.Submissions
                    .OrderByDescending(s => s.SubmitId)
                    .Select(s => new { s.SubmitId })
                    .FirstOrDefaultAsync()
                ?? new { SubmitId = 0 };

            // Add new submissions
            await foreach (var upstreamItem in weasylUserClient.GetMyGallerySubmissionsAsync())
            {
                if (upstreamItem.submitid > newestKnownSubmission.SubmitId)
                    await crowmaskCache.GetSubmissionAsync(upstreamItem.submitid);
                else
                    break;
            }

            // Update existing journals
            var cachedJournals = await context.Journals
                .Where(j => j.PostedAt >= cutoff)
                .ToListAsync();

            foreach (var journal in cachedJournals)
            {
                if (journal.Stale)
                    await crowmaskCache.GetJournalAsync(journal.JournalId);
            }

            // Find ID of newest known journal
            var newestKnownJournal =
                cachedJournals
                    .OrderByDescending(j => j.JournalId)
                    .Select(j => new { j.JournalId })
                    .FirstOrDefault()
                ?? await context.Journals
                    .OrderByDescending(j => j.JournalId)
                    .Select(j => new { j.JournalId })
                    .FirstOrDefaultAsync()
                ?? new { JournalId = 0 };

            // Add new journals
            await foreach (int journalId in weasylUserClient.GetMyJournalIdsAsync())
            {
                if (journalId > newestKnownJournal.JournalId)
                    await crowmaskCache.GetJournalAsync(journalId);
                else
                    break;
            }
        }
    }
}
