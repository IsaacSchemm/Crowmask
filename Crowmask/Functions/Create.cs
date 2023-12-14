using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Crowmask.Models;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.Functions
{
    public class Create(CrowmaskDbContext context)
    {
        [FunctionName("Create")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string actor = "https://crowmask20231213.azurewebsites.net/api/actor";

            var date = DateTime.UtcNow;

            var apObject = new APObject(
                id: "https://crowmask20231213.azurewebsites.net/api/post/4560",
                type: "Note",
                attributedTo: actor,
                content: $"Test B - {DateTimeOffset.UtcNow}",
                published: date,
                to: ["https://www.w3.org/ns/activitystreams#Public"],
                cc: [$"{actor}/followers"]);

            var post = new Post
            {
                Id = Guid.NewGuid().ToString(),
                ContentsJson = JsonSerializer.Serialize(apObject),
                CreatedAt = date
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var activity = new Activity(
                type: "Create",
                id: "https://crowmask20231213.azurewebsites.net/api/post/1230",
                actor: actor,
                published: date,
                to: ["https://www.w3.org/ns/activitystreams#Public"],
                cc: [$"{actor}/followers"],
                @object: apObject);

            var storedActivity = new Post
            {
                Id = Guid.NewGuid().ToString(),
                ContentsJson = JsonSerializer.Serialize(activity),
                CreatedAt = date
            };
            context.Posts.Add(storedActivity);
            await context.SaveChangesAsync();

            var followers = await context.Followers.ToListAsync();
            followers.Add(new Follower
            {
                Actor = "https://microblog.lakora.us"
            });

            foreach (var follower in followers)
            {
                var a1 = activity with { cc = [follower.Actor] };
                await Requests.SendAsync(actor, follower.Actor, a1);
            }

            return new OkObjectResult("test");
        }
    }
}
