using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrosspostSharp3.Weasyl;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.Remote;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crowmask.Functions
{
    public class ShortUpdate(CrowmaskCache crowmaskCache, CrowmaskDbContext context, OutboundActivityProcessor outboundActivityProcessor, WeasylClient weasylClient)
    {
        [FunctionName("ShortUpdate")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var user = await context.Users
                .AsNoTracking()
                .SingleAsync();

            var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromDays(14);

            await foreach (var submission in weasylClient.GetUserGallerySubmissionsAsync(user.Username))
            {
                log.LogInformation($"submitid: {submission.submitid}");

                if (submission.posted_at < cutoff)
                {
                    break;
                }

                await crowmaskCache.GetSubmission(submission.submitid);
            }

            var cachedSubmissions = await context.Submissions
                .AsNoTracking()
                .Where(s => s.PostedAt >= cutoff)
                .Select(s => new { s.SubmitId })
                .ToListAsync();

            foreach (var submission in cachedSubmissions)
            {
                log.LogInformation($"--submitid: {submission.SubmitId}");

                await crowmaskCache.GetSubmission(submission.SubmitId);
            }

            await outboundActivityProcessor.ProcessOutboundActivities();
        }
    }
}
