using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Crowmask.ActivityPub;
using Crowmask.Data;
using System;

namespace Crowmask.Functions
{
    public static class Submissions
    {
        [FunctionName("Submissions")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "submissions/{submitid}")] HttpRequest req,
            int submitid,
            ILogger log)
        {
            var submission = new Submission
            {
                Description = "This is the <b>description</b>",
                FriendsOnly = false,
                Id = Guid.NewGuid(),
                PostedAt = new DateTimeOffset(2023, 12, 9, 12, 0, 0, TimeSpan.Zero),
                RatingId = Submission.Rating.General,
                SubmitId = submitid,
                SubtypeId = Submission.Subtype.Visual,
                Tags = [
                    new SubmissionTag { Tag = "tag5" },
                    new SubmissionTag { Tag = "tag6" }
                ],
                Title = "The Title",
                UpdatedAt = new DateTimeOffset(2023, 12, 13, 12, 0, 0, TimeSpan.Zero),
                Media = [
                    new SubmissionMedia { Url = "https://cdn.weasyl.com/~lizardsocks/submissions/2326525/c774c4f03f37127be0c8183a95509b343a4d55e8602a1f6a05936824914203db/lizardsocks-nervous-odri.png" }
                ]
            };

            var apObject = AP.AsObject(
                Domain.AsNote(submission));

            return new JsonResult(apObject);
        }
    }
}
