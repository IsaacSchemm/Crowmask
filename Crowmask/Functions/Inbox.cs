using Crowmask.ActivityPub;
using Crowmask.Cache;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Inbox(CrowmaskDbContext context, IAdminActor adminActor, ICrowmaskHost host, Requester requester, Translator translator)
    {
        private bool TryExtractSubmitId(string objectId, out int submitid)
        {
            if (Uri.TryCreate(objectId, UriKind.Absolute, out Uri idUri)
                && idUri.Host == host.Hostname
                && idUri.AbsolutePath.Split('/') is string[] arr
                && arr.Length >= 4
                && int.TryParse(arr[3], out int s))
            {
                submitid = s;
                return true;
            }
            else
            {
                submitid = 0;
                return false;
            }
        }

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
                string actor = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();

                var existing = await context.Followers
                    .Where(f => f.ActorId == actor)
                    .SingleOrDefaultAsync();

                var actorObj = await requester.FetchActorAsync(actor);

                if (existing != null)
                {
                    existing.MostRecentFollowId = id;
                }
                else
                {
                    context.Followers.Add(new Follower
                    {
                        Id = Guid.NewGuid(),
                        UserId = CrowmaskCache.WEASYL_MIRROR_ACTOR,
                        ActorId = actor,
                        MostRecentFollowId = id,
                        Inbox = actorObj.Inbox,
                        SharedInbox = actorObj.SharedInbox
                    });
                }

                Guid guid = Guid.NewGuid();

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = actorObj.Inbox,
                    JsonBody = AP.SerializeWithContext(translator.AcceptFollow(id)),
                    StoredAt = DateTimeOffset.UtcNow
                });
                await context.SaveChangesAsync();

                return new StatusCodeResult(202);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                var followers = await context.Followers
                    .Where(f => f.MostRecentFollowId == objectId)
                    .ToListAsync();

                foreach (var follower in followers)
                {
                    context.Followers.Remove(follower);
                    await context.SaveChangesAsync();
                }

                return new StatusCodeResult(202);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Like")
            {
                var adminActorObject = await requester.FetchActorAsync(adminActor.Id);

                string actorUrl = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();
                var actor = await requester.FetchActorAsync(actorUrl);

                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                if (TryExtractSubmitId(objectId, out int submitid))
                {
                    var submission = await context.Submissions.FindAsync(submitid);
                    if (submission != null)
                    {
                        string jsonBody = AP.SerializeWithContext(
                            translator.CreateLikeNotification(
                                submission,
                                actor));
                        context.OutboundActivities.Add(new OutboundActivity
                        {
                            Id = Guid.NewGuid(),
                            Inbox = adminActorObject.Inbox,
                            JsonBody = jsonBody,
                            StoredAt = DateTimeOffset.UtcNow
                        });
                        await context.SaveChangesAsync();

                        return new StatusCodeResult(202);
                    }
                }

                return new StatusCodeResult(204);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Announce")
            {
                var adminActorObject = await requester.FetchActorAsync(adminActor.Id);

                string actorUrl = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();
                var actor = await requester.FetchActorAsync(actorUrl);

                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                if (TryExtractSubmitId(objectId, out int submitid))
                {
                    var submission = await context.Submissions.FindAsync(submitid);
                    if (submission != null)
                    {
                        string jsonBody = AP.SerializeWithContext(
                            translator.CreateShareNotification(
                                submission,
                                actor));
                        context.OutboundActivities.Add(new OutboundActivity
                        {
                            Id = Guid.NewGuid(),
                            Inbox = adminActorObject.Inbox,
                            JsonBody = jsonBody,
                            StoredAt = DateTimeOffset.UtcNow
                        });
                        await context.SaveChangesAsync();

                        return new StatusCodeResult(202);
                    }
                }

                return new StatusCodeResult(204);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                var adminActorObject = await requester.FetchActorAsync(adminActor.Id);

                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                string postJson = await requester.GetJsonAsync(new Uri(objectId));
                JArray postExpansion = JsonLdProcessor.Expand(JObject.Parse(postJson));

                string actorUrl = postExpansion[0]["https://www.w3.org/ns/activitystreams#attributedTo"][0]["@id"].Value<string>();
                var actor = await requester.FetchActorAsync(actorUrl);

                foreach (var inReplyTo in postExpansion[0]["https://www.w3.org/ns/activitystreams#inReplyTo"])
                {
                    string inReplyToId = inReplyTo["@id"].Value<string>();
                    if (TryExtractSubmitId(inReplyToId, out int submitid))
                    {
                        var submission = await context.Submissions.FindAsync(submitid);
                        if (submission != null)
                        {
                            string jsonBody = AP.SerializeWithContext(
                                translator.CreateReplyNotification(
                                    submission,
                                    actor,
                                    objectId));
                            context.OutboundActivities.Add(new OutboundActivity
                            {
                                Id = Guid.NewGuid(),
                                Inbox = adminActorObject.Inbox,
                                JsonBody = jsonBody,
                                StoredAt = DateTimeOffset.UtcNow
                            });
                            await context.SaveChangesAsync();

                            return new StatusCodeResult(202);
                        }
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
