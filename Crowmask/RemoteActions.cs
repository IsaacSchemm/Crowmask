using Crowmask.DomainModeling;
using Crowmask.Formats.ActivityPub;
using Crowmask.Library.Remote;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask
{
    /// <summary>
    /// Provides a way for the inbox handler to send messages to a remote
    /// ActivityPub actor. Outgoing messages will be stored in the Crowmask
    /// database and sent by RefreshUpstream.
    /// </summary>
    public class RemoteActions(IAdminActor adminActor, DatabaseActions databaseActions, Requester requester, Translator translator)
    {
        private async Task SendToAdminActorAsync(IDictionary<string, object> activityPubObject)
        {
            var adminActorDetails = await requester.FetchActorAsync(adminActor.Id);
            await databaseActions.AddOutboundActivityAsync(activityPubObject, adminActorDetails);
        }

        /// <summary>
        /// Sends an Accept activity to accept a follow request.( Crowmask
        /// automatically accepts all incoming follow requests, although the
        /// RefreshUpstream timer may lead to a delay of up to 10 minutes.)
        /// </summary>
        /// <param name="activityPubObject"></param>
        /// <returns></returns>
        public async Task AcceptFollowAsync(string objectId, RemoteActor actor)
        {
            await databaseActions.AddOutboundActivityAsync(
                translator.AcceptFollow(objectId),
                actor);
        }

        /// <summary>
        /// Notifies the admin actor of likes, boosts, and replies on posts,
        /// and removes old notifications of likes, boosts, and replies that
        /// have since been deleted.
        /// </summary>
        /// <param name="oldPost">The post as it was before making changes</param>
        /// <param name="newPost">The post as it is now</param>
        /// <returns></returns>
        public async Task UpdateAdminActorNotificationsAsync(Post oldPost, Post newPost)
        {
            var added = newPost.Interactions.Except(oldPost.Interactions);
            foreach (var interaction in added)
                await SendToAdminActorAsync(translator.PrivateNoteToCreate(newPost, interaction));

            var removed = oldPost.Interactions.Except(newPost.Interactions);
            foreach (var interaction in removed)
                await SendToAdminActorAsync(translator.PrivateNoteToDelete(oldPost, interaction));
        }
    }
}
