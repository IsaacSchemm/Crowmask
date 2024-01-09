using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.IdMapping;
using Crowmask.Remote;
using Crowmask.Signatures;
using JsonLD.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
    public class Inbox(ActivityStreamsIdMapper mapper, CrowmaskCache cache, CrowmaskDbContext context, DatabaseActions databaseActions, IAdminActor adminActor, ICrowmaskHost host, MastodonVerifier mastodonVerifier, RemoteActions remoteActions, Requester requester, Translator translator)
    {
        [Obsolete]
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

        [Obsolete]
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

        [Obsolete]
        private async Task SendToAdminActorAsync(IDictionary<string, object> activityPubObject)
        {
            var adminActorDetails = await requester.FetchActorAsync(adminActor.Id);
            await databaseActions.AddOutboundActivityAsync(activityPubObject, adminActorDetails);
        }

        [Obsolete]
        private async Task CreateEngagementNotificationAsync(Guid id, Submission submission)
        {
            var result = await cache.GetSubmissionAsync(submission.SubmitId);
            if (result is CacheResult.PostResult cacheResult && cacheResult.Post is Post post)
                foreach (var interaction in post.Interactions)
                    if (interaction.Id == id)
                        await SendToAdminActorAsync(translator.PrivateNoteToCreate(post, interaction));
        }

        [Obsolete]
        private async Task DeleteEngagementNotificationAsync(Guid id, Submission submission)
        {
            var result = await cache.GetSubmissionAsync(submission.SubmitId);
            if (result is CacheResult.PostResult cacheResult && cacheResult.Post is Post post)
                foreach (var interaction in post.Interactions)
                    if (interaction.Id == id)
                        await SendToAdminActorAsync(translator.PrivateNoteToDelete(post, interaction));
        }

        [Obsolete]
        private async Task CreateEngagementNotificationAsync(Guid id, Journal journal)
        {
            var result = await cache.GetJournalAsync(journal.JournalId);
            if (result is CacheResult.PostResult cacheResult && cacheResult.Post is Post post)
                foreach (var interaction in post.Interactions)
                    if (interaction.Id == id)
                        await SendToAdminActorAsync(translator.PrivateNoteToCreate(post, interaction));
        }

        [Obsolete]
        private async Task DeleteEngagementNotificationAsync(Guid id, Journal journal)
        {
            var result = await cache.GetJournalAsync(journal.JournalId);
            if (result is CacheResult.PostResult cacheResult && cacheResult.Post is Post post)
                foreach (var interaction in post.Interactions)
                    if (interaction.Id == id)
                        await SendToAdminActorAsync(translator.PrivateNoteToDelete(post, interaction));
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

                await databaseActions.AddFollowAsync(objectId, actor);
                await remoteActions.AcceptFollowAsync(objectId, actor);

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                await databaseActions.RemoveFollowAsync(objectId);

                await foreach (var post in cache.GetRelevantCachedPostsAsync(objectId))
                {
                    foreach (var boost in post.boosts)
                        if (boost.actor_id == actor.Id && boost.announce_id == objectId)
                            await databaseActions.RemoveInteractionAsync(post.identifier, boost.id);
                    foreach (var like in post.likes)
                        if (like.actor_id == actor.Id && like.like_id == objectId)
                            await databaseActions.RemoveInteractionAsync(post.identifier, like.id);

                    if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                        await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Like")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                if (mapper.GetJointIdentifier(objectId) is not JointIdentifier identifier)
                    return req.CreateResponse(HttpStatusCode.NoContent);

                if (await cache.GetCachedPostAsync(identifier) is not CacheResult.PostResult pr || pr.Post is not Post post)
                    return req.CreateResponse(HttpStatusCode.NoContent);

                foreach (var like in post.likes)
                    if (like.actor_id == actor.Id)
                        await databaseActions.RemoveInteractionAsync(identifier, like.id);

                await databaseActions.AddLikeAsync(identifier, activityId, actor);

                if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                    await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Announce")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                string objectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                if (mapper.GetJointIdentifier(objectId) is not JointIdentifier identifier)
                    return req.CreateResponse(HttpStatusCode.NoContent);

                if (await cache.GetCachedPostAsync(identifier) is not CacheResult.PostResult pr || pr.Post is not Post post)
                    return req.CreateResponse(HttpStatusCode.NoContent);

                foreach (var boost in post.boosts)
                    if (boost.actor_id == actor.Id)
                        await databaseActions.RemoveInteractionAsync(identifier, boost.id);

                await databaseActions.AddBoostAsync(identifier, activityId, actor);

                if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                    await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);

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

                        await CreateEngagementNotificationAsync(guid, submission);
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

                        await CreateEngagementNotificationAsync(guid, journal);
                    }
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                string deletedObjectId = expansion[0]["https://www.w3.org/ns/activitystreams#object"][0]["@id"].Value<string>();

                await foreach (var post in cache.GetRelevantCachedPostsAsync(deletedObjectId))
                {
                    foreach (var reply in post.replies)
                        if (reply.actor_id == actor.Id && reply.object_id == deletedObjectId)
                            await databaseActions.RemoveInteractionAsync(post.identifier, reply.id);

                    if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                        await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
        }
    }
}
