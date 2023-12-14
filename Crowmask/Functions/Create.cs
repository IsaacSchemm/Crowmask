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
        private record ObjectToCreate(string type, string content);
        private record ObjectToStore(string attributedTo, string published, string[] to, string[] cc, string type, string content);

        [FunctionName("Create")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string actor = "https://crowmask20231213.azurewebsites.net/api/actor";

            var body = new ObjectToCreate(
                type: "Note",
                content: $"Test A - {DateTimeOffset.UtcNow}");

            var date = DateTime.UtcNow;

            var post = new Post
            {
                Id = Guid.NewGuid().ToString(),
                ContentsJson = JsonSerializer.Serialize(new ObjectToStore(
                    attributedTo: actor,
                    published: date.ToString("r"),
                    to: ["https://www.w3.org/ns/activitystreams#Public"],
                    cc: [$"{actor}/followers"],
                    content: body.content,
                    type: body.type)),
                CreatedAt = date
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(post.ContentsJson);
            obj.Add("id", post.Id);

            var activity = new Dictionary<string, object>
            {
                ["@context"] = "https://www.w3.org/ns/activitystreams",
                ["type"] = "Create",
                ["published"] = date.ToString("r"),
                ["actor"] = actor,
                ["to"] = new string[] { "https://www.w3.org/ns/activitystreams#Public" },
                ["cc"] = new string[] { $"{actor}/followers" },
                ["content"] = body.content,
                ["type"] = body.type,
                ["object"] = obj
            };

            var storedActivity = new Post
            {
                Id = Guid.NewGuid().ToString(),
                ContentsJson = JsonSerializer.Serialize(activity),
                CreatedAt = date
            };
            context.Posts.Add(storedActivity);
            await context.SaveChangesAsync();

            activity["id"] = storedActivity.Id;

            var followers = await context.Followers.ToListAsync();
            followers.Add(new Follower
            {
                Actor = "https://microblog.lakora.us"
            });

            foreach (var follower in followers)
            {
                obj["id"] = $"{actor}/post/{activity["id"]}";
                obj["cc"] = new[] { follower.Actor };
                await Requests.SendAsync(actor, follower.Actor, activity);
            }

            return new OkObjectResult("test");
        }
    }
}
