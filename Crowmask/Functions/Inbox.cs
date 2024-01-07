using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Remote;
using Crowmask.Signatures;
using JsonLD.Core;
using Microsoft.Azure.Cosmos.Linq;
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
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Inbox(CrowmaskCache cache, CrowmaskDbContext context, IAdminActor adminActor, ICrowmaskHost host, MastodonVerifier mastodonVerifier, Notifier notifier, Requester requester, Translator translator)
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

        private async Task<Journal> FindJournalByObjectIdAsync(string objectId)
        {
            string prefix = $"https://{host.Hostname}/api/journals/";
            if (!objectId.StartsWith(prefix))
                return null;

            string remainder = objectId[prefix.Length..];
            if (!int.TryParse(remainder, out int journalid))
                return null;

            return await context.Journals.FindAsync(journalid);
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

        private async Task CreateSubmissionEngagementNotification(Guid id, int submitid)
        {
            var result = await cache.GetSubmissionAsync(submitid);
            foreach (var post in result.AsList)
                foreach (var engagement in EngagementModule.GetAll(post))
                    if (engagement.Id == id)
                        await SendToAdminActorAsync(notifier.CreatePostEngagementNotification(post, engagement));
        }

        private async Task CreateJournalEngagementNotification(Guid id, int journalid)
        {
            var result = await cache.GetJournalAsync(journalid);
            foreach (var post in result.AsList)
                foreach (var engagement in EngagementModule.GetAll(post))
                    if (engagement.Id == id)
                        await SendToAdminActorAsync(notifier.CreatePostEngagementNotification(post, engagement));
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

                await foreach (var j in context.GetRelevantJournalsAsync(objectId))
                {
                    foreach (var b in j.Boosts.ToList())
                        if (b.ActorId == actor.Id && b.ActivityId == objectId)
                            j.Boosts.Remove(b);
                    foreach (var l in j.Likes.ToList())
                        if (l.ActorId == actor.Id && l.ActivityId == objectId)
                            j.Likes.Remove(l);
                }

                await foreach (var s in context.GetRelevantSubmissionsAsync(objectId))
                {
                    foreach (var b in s.Boosts.ToList())
                        if (b.ActorId == actor.Id && b.ActivityId == objectId)
                            s.Boosts.Remove(b);
                    foreach (var l in s.Likes.ToList())
                        if (l.ActorId == actor.Id && l.ActivityId == objectId)
                            s.Likes.Remove(l);
                }

                await context.SaveChangesAsync();

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Like")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                if (await FindSubmissionByObjectIdAsync(objectId) is Submission submission)
                {
                    foreach (var like in submission.Likes.ToList())
                        if (like.ActorId == actor.Id)
                            submission.Likes.Remove(like);

                    var guid = Guid.NewGuid();

                    submission.Likes.Add(new SubmissionLike
                    {
                        Id = guid,
                        AddedAt = DateTimeOffset.UtcNow,
                        ActivityId = activityId,
                        ActorId = actor.Id,
                    });

                    await context.SaveChangesAsync();

                    await CreateSubmissionEngagementNotification(
                        guid,
                        submission.SubmitId);
                }

                if (await FindJournalByObjectIdAsync(objectId) is Journal journal)
                {
                    foreach (var like in journal.Likes.ToList())
                        if (like.ActorId == actor.Id)
                            journal.Likes.Remove(like);

                    var guid = Guid.NewGuid();

                    journal.Likes.Add(new JournalLike
                    {
                        Id = guid,
                        AddedAt = DateTimeOffset.UtcNow,
                        ActivityId = activityId,
                        ActorId = actor.Id,
                    });

                    await context.SaveChangesAsync();

                    await CreateJournalEngagementNotification(
                        guid,
                        journal.JournalId);
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Announce")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                if (await FindSubmissionByObjectIdAsync(objectId) is Submission submission)
                {
                    var guid = Guid.NewGuid();

                    submission.Boosts.Add(new SubmissionBoost
                    {
                        Id = guid,
                        AddedAt = DateTimeOffset.UtcNow,
                        ActivityId = activityId,
                        ActorId = actor.Id,
                    });

                    await context.SaveChangesAsync();

                    await CreateSubmissionEngagementNotification(
                        guid,
                        submission.SubmitId);
                }

                if (await FindJournalByObjectIdAsync(objectId) is Journal journal)
                {
                    var guid = Guid.NewGuid();

                    journal.Boosts.Add(new JournalBoost
                    {
                        Id = guid,
                        AddedAt = DateTimeOffset.UtcNow,
                        ActivityId = activityId,
                        ActorId = actor.Id,
                    });

                    await context.SaveChangesAsync();

                    await CreateJournalEngagementNotification(
                        guid,
                        journal.JournalId);
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
                        var guid = Guid.NewGuid();

                        submission.Replies.Add(new SubmissionReply
                        {
                            Id = guid,
                            AddedAt = DateTimeOffset.UtcNow,
                            ActorId = actor.Id,
                            ObjectId = replyId,
                        });

                        await context.SaveChangesAsync();

                        await CreateSubmissionEngagementNotification(
                            guid,
                            submission.SubmitId);
                    }

                    if (await FindJournalByObjectIdAsync(objectId) is Journal journal)
                    {
                        var guid = Guid.NewGuid();

                        journal.Replies.Add(new JournalReply
                        {
                            Id = guid,
                            AddedAt = DateTimeOffset.UtcNow,
                            ActorId = actor.Id,
                            ObjectId = replyId,
                        });

                        await context.SaveChangesAsync();

                        await CreateJournalEngagementNotification(
                            guid,
                            journal.JournalId);
                    }
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                string deletedObjectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                await foreach (var j in context.GetRelevantJournalsAsync(deletedObjectId))
                    foreach (var b in j.Replies.ToList())
                        if (b.ActorId == actor.Id && b.ObjectId == deletedObjectId)
                            j.Replies.Remove(b);

                await foreach (var s in context.GetRelevantSubmissionsAsync(deletedObjectId))
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
