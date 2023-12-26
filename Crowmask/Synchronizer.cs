﻿using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.Weasyl;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask
{
    public class Synchronizer(CrowmaskCache crowmaskCache, CrowmaskDbContext context, WeasylClient weasylClient)
    {
        public async Task SynchronizeAsync(DateTimeOffset cutoff)
        {
            var user = await context.GetUserAsync();

            await foreach (var submission in weasylClient.GetUserGallerySubmissionsAsync(user.Username))
            {
                if (submission.posted_at < cutoff)
                {
                    break;
                }

                await crowmaskCache.GetSubmission(submission.submitid);
            }

            var cachedSubmissions = await context.Submissions
                .Where(s => s.PostedAt >= cutoff)
                .ToListAsync();

            foreach (var submission in cachedSubmissions)
            {
                if (submission.Stale)
                {
                    await crowmaskCache.GetSubmission(submission.SubmitId);
                }
            }
        }
    }
}
