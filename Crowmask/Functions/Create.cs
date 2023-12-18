using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Crowmask.Data;
using Crowmask.ActivityPub;

namespace Crowmask.Functions
{
    public class Create(CrowmaskDbContext context)
    {
        [FunctionName("Create")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var submission = new Submission
            {
                Description = "This is the <b>description</b>",
                FriendsOnly = false,
                PostedAt = DateTimeOffset.UtcNow.AddMinutes(-2),
                RatingId = Submission.Rating.General,
                SubmitId = 7,
                SubtypeId = Submission.Subtype.Visual,
                Tags = [
                    new SubmissionTag { Tag = "tag1" },
                    new SubmissionTag { Tag = "tag2" }
                ],
                Title = "The Title",
                Media = [
                    new SubmissionMedia { Url = "https://cdn.weasyl.com/~lizardsocks/submissions/2326525/c774c4f03f37127be0c8183a95509b343a4d55e8602a1f6a05936824914203db/lizardsocks-nervous-odri.png" }
                ],
                CacheRefreshAttemptedAt = DateTimeOffset.UtcNow.AddHours(-1),
                CacheRefreshSucceededAt = DateTimeOffset.UtcNow.AddHours(-1)
            };

            var apObject = AP.AsObject(
                Domain.AsNote(submission));

            var activity = AP.AsActivity(
                Domain.AsCreate(submission),
                Recipient.NewActorRecipient("https://microblog.lakora.us"));

            await Requests.SendAsync(AP.ACTOR, "https://microblog.lakora.us", activity);

            return new OkObjectResult("test");
        }
    }
}
