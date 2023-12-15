using System;
using System.IO;
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
    public class Delete(CrowmaskDbContext context)
    {
        [FunctionName("Delete")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var submission = new Submission
            {
                Description = "This is the <b>description</b>",
                FriendsOnly = false,
                Id = Guid.NewGuid(),
                PostedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                RatingId = Submission.Rating.General,
                SubmitId = 2326525,
                SubtypeId = Submission.Subtype.Visual,
                Tags = ["tag1", "tag2"],
                Title = "The Title",
                UpdatedAt = DateTimeOffset.UtcNow,
                Urls = ["https://cdn.weasyl.com/~lizardsocks/submissions/2326525/c774c4f03f37127be0c8183a95509b343a4d55e8602a1f6a05936824914203db/lizardsocks-nervous-odri.png"]
            };

            var activity = AP.AsActivity(
                Domain.AsDelete(new DeleteActivity
                {
                    Id = Guid.NewGuid(),
                    SubmitId = 1,
                    PublishedAt = DateTimeOffset.UtcNow
                }),
                Recipient.NewActorRecipient("https://microblog.lakora.us"));

            await Requests.SendAsync(AP.ACTOR, "https://microblog.lakora.us", activity);

            return new OkObjectResult("test");
        }
    }
}
