using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.Remote;
using JsonLD.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Inbox(CrowmaskDbContext context)
    {
        [FunctionName("Inbox")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/actor/inbox")] HttpRequest req,
            ILogger log)
        {
            using var sr = new StreamReader(req.Body);
            string json = await sr.ReadToEndAsync();

            JObject document = JObject.Parse(json);
            JArray expansion = JsonLdProcessor.Expand(document);

            string type = expansion[0]["@type"][0].Value<string>();

            if (type == "https://www.w3.org/ns/activitystreams#Follow")
            {
                string id = expansion[0]["@id"].Value<string>();

                bool exists = await context.Followers
                    .Where(f => f.FollowId == id)
                    .AnyAsync();

                if (exists)
                    return new StatusCodeResult(204);

                string actor = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();

                var actorObj = await Requests.FetchActorAsync(actor);

                Guid guid = Guid.NewGuid();

                context.Followers.Add(new Follower
                {
                    Id = Guid.NewGuid(),
                    ActorId = actor,
                    FollowId = id,
                    Inbox = actorObj.Inbox,
                    SharedInbox = actorObj.SharedInbox
                });

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    ExternalId = guid,
                    Inbox = actorObj.Inbox,
                    JsonBody = AP.SerializeWithContext(AP.AcceptFollow(guid, id)),
                    StoredAt = DateTimeOffset.UtcNow
                });
                await context.SaveChangesAsync();

                return new StatusCodeResult(202);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                var followers = await context.Followers
                    .Where(f => f.FollowId == objectId)
                    .ToListAsync();

                foreach (var follower in followers)
                {
                    context.Followers.Remove(follower);
                    await context.SaveChangesAsync();
                }

                return new StatusCodeResult(202);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                string id = expansion[0]["@id"].Value<string>();

                foreach (var inReplyTo in expansion[0]["https://www.w3.org/ns/activitystreams#inReplyTo"])
                {
                    string inReplyToId = inReplyTo["@id"].Value<string>();
                    if (inReplyToId.StartsWith(AP.HOST))
                    {
                        context.PrivateAnnouncements.Add(new PrivateAnnouncement
                        {
                            Id = Guid.NewGuid(),
                            AnnouncedObjectId = inReplyToId,
                            PublishedAt = DateTimeOffset.UtcNow
                        });
                        await context.SaveChangesAsync();
                    }
                }
                return new StatusCodeResult(204);
            }
            else
            {
                return new StatusCodeResult(204);
            }
        }
    }
}
