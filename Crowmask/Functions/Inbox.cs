using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.DomainModeling;
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
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Inbox(CrowmaskDbContext context, IAdminActor adminActor, ICrowmaskHost host, Notifier notifier, Requester requester, Translator translator)
    {
        private async Task<Submission> FindSubmissionByObjectIdAsync(string objectId)
        {
            string prefix = $"https://{host.Hostname}/api/submissions/";
            if (!objectId.StartsWith(prefix))
                return null;

            string remainder = objectId[prefix.Length..];
            if (!int.TryParse(remainder, out int submitid))
                return null;

            return await context.Submissions.FindAsync(submitid);
        }

        private async Task SendToAdminActorAsync(IDictionary<string, object> activityPubObject)
        {
            var adminActorDetails = await requester.FetchActorAsync(adminActor.Id);

            context.OutboundActivities.Add(new OutboundActivity
            {
                Id = Guid.NewGuid(),
                Inbox = adminActorDetails.Inbox,
                JsonBody = AP.SerializeWithContext(activityPubObject),
                StoredAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
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
                string objectId = expansion[0]["@id"].Value<string>();
                string actorId = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();
                var actor = await requester.FetchActorAsync(actorId);

                var existing = await context.Followers
                    .Where(f => f.ActorId == actor.Id)
                    .SingleOrDefaultAsync();

                if (existing != null)
                {
                    existing.MostRecentFollowId = objectId;
                }
                else
                {
                    context.Followers.Add(new Follower
                    {
                        Id = Guid.NewGuid(),
                        ActorId = actor.Id,
                        MostRecentFollowId = objectId,
                        Inbox = actor.Inbox,
                        SharedInbox = actor.SharedInbox
                    });
                }

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = actor.Inbox,
                    JsonBody = AP.SerializeWithContext(
                        translator.AcceptFollow(
                            objectId)),
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
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();
                string actorId = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();
                var actor = await requester.FetchActorAsync(actorId);

                if (await FindSubmissionByObjectIdAsync(objectId) is Submission submission)
                {
                    await SendToAdminActorAsync(
                        notifier.CreateLikeNotification(
                            submission,
                            actor.Id,
                            actor.Name ?? actor.Id));
                }

                return new StatusCodeResult(202);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Announce")
            {
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();
                string actorId = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();
                var actor = await requester.FetchActorAsync(actorId);

                if (await FindSubmissionByObjectIdAsync(objectId) is Submission submission)
                {
                    await SendToAdminActorAsync(
                        notifier.CreateShareNotification(
                            submission,
                            actor.Id,
                            actor.Name ?? actor.Id));
                }

                return new StatusCodeResult(202);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                string replyId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                string replyJson = await requester.GetJsonAsync(new Uri(replyId));
                JArray replyExpansion = JsonLdProcessor.Expand(JObject.Parse(replyJson));

                string actorId = replyExpansion[0]["https://www.w3.org/ns/activitystreams#attributedTo"][0]["@id"].Value<string>();
                var actor = await requester.FetchActorAsync(actorId);

                foreach (var inReplyTo in replyExpansion[0]["https://www.w3.org/ns/activitystreams#inReplyTo"])
                {
                    string objectId = inReplyTo["@id"].Value<string>();
                    if (await FindSubmissionByObjectIdAsync(objectId) is Submission submission)
                    {
                        await SendToAdminActorAsync(
                            notifier.CreateReplyNotification(
                                submission,
                                actor.Id,
                                actor.Name ?? actor.Id,
                                replyId));
                    }
                }

                return new StatusCodeResult(202);
            }
            else
            {
                return new StatusCodeResult(204);
            }
        }
    }
}
