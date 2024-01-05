using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Remote;
using Crowmask.Signatures;
using JsonLD.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Crowmask.Functions
{
    public class Inbox(CrowmaskDbContext context, IAdminActor adminActor, ICrowmaskHost host, MastodonVerifier mastodonVerifier, Notifier notifier, Requester requester, Translator translator)
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

        [Function("Inbox")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/actor/inbox")] HttpRequestData req)
        {
            using var sr = new StreamReader(req.Body);
            string json = await sr.ReadToEndAsync();

            JObject document = JObject.Parse(json);
            JArray expansion = JsonLdProcessor.Expand(document);

            string type = expansion[0]["@type"][0].Value<string>();

            string actorId = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();
            var actor = await requester.FetchActorAsync(actorId);

            var signatureVerificationResult = mastodonVerifier.VerifyRequestSignature(
                new IncomingRequest(
                    new HttpMethod(req.Method),
                    req.Url,
                    req.Headers),
                verificationKey: actor);

            if (signatureVerificationResult != NSign.VerificationResult.SuccessfullyVerified)
                return req.CreateResponse(HttpStatusCode.Forbidden);

            if (type == "https://www.w3.org/ns/activitystreams#Follow")
            {
                string objectId = expansion[0]["@id"].Value<string>();

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

                await SendToAdminActorAsync(
                    notifier.CreateFollowNotification(
                        actor.Id,
                        actor.Name ?? actor.Id));

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                var followers = context.Followers
                    .Where(i => i.MostRecentFollowId == objectId)
                    .AsAsyncEnumerable();
                await foreach (var i in followers)
                    context.Followers.Remove(i);

                var boosted = context.Submissions
                    .FromSqlRaw($"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Boosts)} WHERE t.ActivityId = {{0}}", objectId)
                    .AsAsyncEnumerable();
                await foreach (var s in boosted)
                    foreach (var b in s.Boosts.ToList())
                        if (b.ActorId == actor.Id && b.ActivityId == objectId)
                            s.Boosts.Remove(b);

                var liked = context.Submissions
                    .FromSqlRaw($"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Likes)} WHERE t.ActivityId = {{0}}", objectId)
                    .AsAsyncEnumerable();
                await foreach (var s in liked)
                    foreach (var l in s.Likes.ToList())
                        if (l.ActorId == actor.Id && l.ActivityId == objectId)
                            s.Likes.Remove(l);

                await context.SaveChangesAsync();

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Like")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                if (await FindSubmissionByObjectIdAsync(objectId) is Submission submission)
                {
                    submission.Likes.Add(new SubmissionLike
                    {
                        Id = Guid.NewGuid(),
                        AddedAt = DateTimeOffset.UtcNow,
                        ActivityId = activityId,
                        ActorId = actor.Id,
                    });

                    await context.SaveChangesAsync();

                    await SendToAdminActorAsync(
                        notifier.CreateLikeNotification(
                            submission,
                            actor.Id,
                            actor.Name ?? actor.Id));
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Announce")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                if (await FindSubmissionByObjectIdAsync(objectId) is Submission submission)
                {
                    submission.Boosts.Add(new SubmissionBoost
                    {
                        Id = Guid.NewGuid(),
                        AddedAt = DateTimeOffset.UtcNow,
                        ActivityId = activityId,
                        ActorId = actor.Id,
                    });

                    await context.SaveChangesAsync();

                    await SendToAdminActorAsync(
                        notifier.CreateShareNotification(
                            submission,
                            actor.Id,
                            actor.Name ?? actor.Id));
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                string replyId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                string replyJson = await requester.GetJsonAsync(new Uri(replyId));
                JArray replyExpansion = JsonLdProcessor.Expand(JObject.Parse(replyJson));

                foreach (var inReplyTo in replyExpansion[0]["https://www.w3.org/ns/activitystreams#inReplyTo"])
                {
                    string objectId = inReplyTo["@id"].Value<string>();
                    if (await FindSubmissionByObjectIdAsync(objectId) is Submission submission)
                    {
                        submission.Replies.Add(new SubmissionReply
                        {
                            Id = Guid.NewGuid(),
                            AddedAt = DateTimeOffset.UtcNow,
                            ActorId = actor.Id,
                            ObjectId = replyId,
                        });

                        await context.SaveChangesAsync();

                        await SendToAdminActorAsync(
                            notifier.CreateReplyNotification(
                                submission,
                                actor.Id,
                                actor.Name ?? actor.Id,
                                replyId));
                    }
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                string deletedObjectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                var submissions = context.Submissions
                    .FromSqlRaw($"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Replies)} WHERE t.ActivityId = {{0}}", deletedObjectId)
                    .AsAsyncEnumerable();
                await foreach (var s in submissions)
                    foreach (var b in s.Replies.ToList())
                        if (b.ActorId == actor.Id && b.ObjectId == deletedObjectId)
                            s.Replies.Remove(b);

                await context.SaveChangesAsync();

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
        }
    }
}
