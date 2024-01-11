using Crowmask.Dependencies.Mapping;
using Crowmask.DomainModeling;
using Crowmask.Library.Cache;
using Crowmask.Library.Remote;
using Crowmask.Library.Signatures;
using JsonLD.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Inbox(ActivityStreamsIdMapper mapper, CrowmaskCache cache, DatabaseActions databaseActions, MastodonVerifier mastodonVerifier, RemoteActions remoteActions, Requester requester)
    {
        /// <summary>
        /// Accepts an ActivityPub message. Supported types are:
        /// <list type="bullet">
        /// <item>Follow (adds the follower and sends back an Accept message)</item>
        /// <item>Undo (removes a follower, like, or boost)</item>
        /// <item>Like (records a like on one of this actor's posts)</item>
        /// <item>Announce (records a boost on one of this actor's posts)</item>
        /// <item>Create (records the ID of a post by another actor, if it's a reply to this actor)</item>
        /// <item>Delete (removes the ID of a post by another actor, if it was a reply to this actor)</item>
        /// </list>
        /// New likes, boosts, and replies also generate private Notes sent to the admin actor. Other actions are ignored.
        /// </summary>
        /// <remarks>
        /// Crowmask does not currently forward activities.
        /// </remarks>
        /// <param name="req"></param>
        /// <returns>
        /// <list type="bullet">
        /// <item>202 Accepted</item>
        /// <item>204 No Content (in some cases where Crowmask takes no action)</item>
        /// <item>400 Bad Request (if more than one activity type is specified)</item>
        /// <item>403 Forbidden (if HTTP validation fails)</item>
        /// </list>
        /// </returns>
        [Function("Inbox")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/actor/inbox")] HttpRequestData req)
        {
            using var sr = new StreamReader(req.Body);
            string json = await sr.ReadToEndAsync();

            // Expand JSON-LD
            // This is important to do, because objects can be replaced with IDs, pretty much anything can be an array, etc.
            JObject document = JObject.Parse(json);
            JArray expansion = JsonLdProcessor.Expand(document);

            // Find out which ActivityPub actor they say they are, and grab that actor's information and public key
            string actorId = expansion[0]["https://www.w3.org/ns/activitystreams#actor"][0]["@id"].Value<string>();
            var actor = await requester.FetchActorAsync(actorId);

            // Verify HTTP signature against the public key
            var signatureVerificationResult = mastodonVerifier.VerifyRequestSignature(
                req.AsIRequest(),
                actor);

            if (signatureVerificationResult != NSign.VerificationResult.SuccessfullyVerified)
                return req.CreateResponse(HttpStatusCode.Forbidden);

            // If we've never seen this inbox before, record it so we can send it Update and Delete messages
            await databaseActions.AddKnownInboxAsync(actor);

            if (expansion[0]["@type"].Count() != 1)
                return req.CreateResponse(HttpStatusCode.BadRequest);

            string type = expansion[0]["@type"][0].Value<string>();

            if (type == "https://www.w3.org/ns/activitystreams#Follow")
            {
                string objectId = expansion[0]["@id"].Value<string>();

                await databaseActions.AddFollowAsync(objectId, actor);
                await remoteActions.AcceptFollowAsync(objectId, actor);

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                foreach (var objectToUndo in expansion[0]["https://www.w3.org/ns/activitystreams#object"])
                {
                    string objectId = objectToUndo["@id"].Value<string>();

                    await databaseActions.RemoveFollowAsync(objectId);

                    // Figure out which post the ID belongs to (if any)
                    await foreach (var post in cache.GetRelevantCachedPostsAsync(objectId))
                    {
                        // If the ID to undo is the ID of a boost or like, then undo it
                        foreach (var boost in post.boosts)
                            if (boost.actor_id == actor.Id && boost.announce_id == objectId)
                                await databaseActions.RemoveInteractionAsync(post.identifier, boost.id);
                        foreach (var like in post.likes)
                            if (like.actor_id == actor.Id && like.like_id == objectId)
                                await databaseActions.RemoveInteractionAsync(post.identifier, like.id);

                        // Remove notifications to the admin actor of now-removed likes and boosts
                        if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                            await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);
                    }
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Like")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                foreach (var objectToLike in expansion[0]["https://www.w3.org/ns/activitystreams#object"])
                {
                    string objectId = objectToLike["@id"].Value<string>();

                    // Parse the Crowmask ID from the object ID / URL, if any
                    if (mapper.GetJointIdentifier(objectId) is not JointIdentifier identifier)
                        return req.CreateResponse(HttpStatusCode.NoContent);

                    // Get the cached post that corresponds to this ID, if any
                    if (await cache.GetCachedPostAsync(identifier) is not CacheResult.PostResult pr || pr.Post is not Post post)
                        return req.CreateResponse(HttpStatusCode.NoContent);

                    // Remove any previous likes on this post by this actor
                    foreach (var like in post.likes)
                        if (like.actor_id == actor.Id)
                            await databaseActions.RemoveInteractionAsync(identifier, like.id);

                    // Add the new like
                    await databaseActions.AddLikeAsync(identifier, activityId, actor);

                    // Notify the admin actor of the new like
                    if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                        await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Announce")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                foreach (var objectToBoost in expansion[0]["https://www.w3.org/ns/activitystreams#object"])
                {
                    string objectId = objectToBoost["@id"].Value<string>();

                    // Parse the Crowmask ID from the object ID / URL, if any
                    if (mapper.GetJointIdentifier(objectId) is not JointIdentifier identifier)
                        return req.CreateResponse(HttpStatusCode.NoContent);

                    // Get the cached post that corresponds to this ID, if any
                    if (await cache.GetCachedPostAsync(identifier) is not CacheResult.PostResult pr || pr.Post is not Post post)
                        return req.CreateResponse(HttpStatusCode.NoContent);

                    // Add the boost to the post, unless it's a boost we already know about
                    if (!post.boosts.Select(boost => boost.announce_id).Contains(activityId))
                        await databaseActions.AddBoostAsync(identifier, activityId, actor);

                    // Notify the admin actor of the new boost
                    if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                        await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                foreach (var reply in expansion[0]["https://www.w3.org/ns/activitystreams#object"])
                {
                    string replyId = reply["@id"].Value<string>();

                    // Fetch the reply itself (Crowmask only supports public replies)
                    string replyJson = await requester.GetJsonAsync(new Uri(replyId));
                    JArray replyExpansion = JsonLdProcessor.Expand(JObject.Parse(replyJson));

                    foreach (var inReplyTo in replyExpansion[0]["https://www.w3.org/ns/activitystreams#inReplyTo"])
                    {
                        // Find the ID of the object that this is in reply to
                        string objectId = inReplyTo["@id"].Value<string>();

                        // Parse the Crowmask ID from the object ID / URL, if any
                        if (mapper.GetJointIdentifier(objectId) is not JointIdentifier identifier)
                            return req.CreateResponse(HttpStatusCode.NoContent);

                        // Get the cached post that corresponds to this ID, if any
                        if (await cache.GetCachedPostAsync(identifier) is not CacheResult.PostResult pr || pr.Post is not Post post)
                            return req.CreateResponse(HttpStatusCode.NoContent);

                        // Add the reply to the post, unless it's a reply we already know about
                        if (!post.replies.Select(reply => reply.object_id).Contains(replyId))
                            await databaseActions.AddReplyAsync(identifier, replyId, actor);

                        // Notify the admin actor of the new boost
                        if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                            await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);
                    }
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                foreach (var deletedObject in expansion[0]["https://www.w3.org/ns/activitystreams#object"])
                {
                    string deletedObjectId = deletedObject["@id"].Value<string>();

                    // Figure out which post the deleted post was in reply to (if any)
                    await foreach (var post in cache.GetRelevantCachedPostsAsync(deletedObjectId))
                    {
                        // If the actor who sent the Delete request was the actor who originally posted the reply, then delete it from our cache
                        foreach (var reply in post.replies)
                            if (reply.actor_id == actor.Id && reply.object_id == deletedObjectId)
                                await databaseActions.RemoveInteractionAsync(post.identifier, reply.id);

                        // Remove notifications to the admin actor of now-removed replies
                        if (await cache.GetCachedPostAsync(post.identifier) is CacheResult.PostResult newData)
                            await remoteActions.UpdateAdminActorNotificationsAsync(post, newData.Post);
                    }
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
