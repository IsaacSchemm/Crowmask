using Crowmask.HighLevel;
using Crowmask.HighLevel.Remote;
using Crowmask.HighLevel.Signatures;
using Crowmask.Interfaces;
using Crowmask.LowLevel;
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
    public class Inbox(
        IApplicationInformation appInfo,
        ActivityStreamsIdMapper mapper,
        InboxHandler inboxHandler,
        MastodonVerifier mastodonVerifier,
        Requester requester)
    {
        private static readonly IEnumerable<JToken> Empty = [];

        /// <summary>
        /// Extracts the submission ID, if any, from an ActivityPub object ID.
        /// </summary>
        /// <param name="objectId">The ActivityPub ID / URL for a Crowmask post</param>
        /// <returns>A submission ID, or null</returns>
        private int? GetSubmitId(string objectId)
        {
            return Uri.TryCreate(objectId, UriKind.Absolute, out Uri uri)
                && int.TryParse(uri.AbsolutePath.Split('/').Last(), out int candidate)
                && mapper.GetObjectId(candidate) == objectId
                    ? candidate
                    : null;
        }

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
            await inboxHandler.AddKnownInboxAsync(actor);

            string type = expansion[0]["@type"].Single().Value<string>();

            if (type == "https://www.w3.org/ns/activitystreams#Follow")
            {
                string objectId = expansion[0]["@id"].Value<string>();

                await inboxHandler.AddFollowAsync(objectId, actor);

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                foreach (var objectToUndo in expansion[0]["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string objectId = objectToUndo["@id"].Value<string>();

                    await inboxHandler.RemoveFollowAsync(objectId);

                    await inboxHandler.RemoveInteractionsAsync([objectId], actor);
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Like")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                foreach (var objectToLike in expansion[0]["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string objectId = objectToLike["@id"].Value<string>();

                    if (Uri.TryCreate(objectId, UriKind.Absolute, out Uri uri) && uri.Host == appInfo.ApplicationHostname)
                    {
                        // Add the new like
                        await inboxHandler.AddInteractionAsync(activityId, type, objectId, actor);
                    }
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Announce")
            {
                string activityId = expansion[0]["@id"].Value<string>();
                foreach (var objectToBoost in expansion[0]["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string objectId = objectToBoost["@id"].Value<string>();

                    if (Uri.TryCreate(objectId, UriKind.Absolute, out Uri uri) && uri.Host == appInfo.ApplicationHostname)
                    {
                        // Add the new boost
                        await inboxHandler.AddInteractionAsync(activityId, type, objectId, actor);
                    }
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                foreach (var obj in expansion[0]["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string replyId = obj["@id"].Value<string>();

                    // Fetch the reply itself (Crowmask only supports public replies)
                    string replyJson = await requester.GetJsonAsync(new Uri(replyId));
                    JArray replyExpansion = JsonLdProcessor.Expand(JObject.Parse(replyJson));

                    var relevantIds = Empty
                        .Concat(replyExpansion[0]["https://www.w3.org/ns/activitystreams#to"] ?? Empty)
                        .Concat(replyExpansion[0]["https://www.w3.org/ns/activitystreams#cc"] ?? Empty)
                        .Concat(replyExpansion[0]["https://www.w3.org/ns/activitystreams#inReplyTo"] ?? Empty)
                        .Select(token => token["@id"].Value<string>());

                    if (relevantIds.Any(id => Uri.TryCreate(id, UriKind.Absolute, out Uri uri) && uri.Host == appInfo.ApplicationHostname))
                    {
                        await inboxHandler.AddMentionAsync(replyId, actor);
                    }
                }

                return req.CreateResponse(HttpStatusCode.Accepted);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                foreach (var deletedObject in expansion[0]["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string deletedObjectId = deletedObject["@id"].Value<string>();

                    await inboxHandler.RemoveMentionsAsync([deletedObjectId], actor);
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
