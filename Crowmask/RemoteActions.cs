using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.DomainModeling;
using Crowmask.Remote;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask
{
    public class RemoteActions(IAdminActor adminActor, CrowmaskCache crowmaskCache, DatabaseActions databaseActions, Requester requester, Translator translator)
    {
        public async Task SendToAdminActorAsync(IDictionary<string, object> activityPubObject)
        {
            var adminActorDetails = await requester.FetchActorAsync(adminActor.Id);
            await databaseActions.AddOutboundActivityAsync(activityPubObject, adminActorDetails);
        }

        public async Task AcceptFollowAsync(string objectId, Requester.RemoteActor actor)
        {
            await databaseActions.AddOutboundActivityAsync(
                translator.AcceptFollow(objectId),
                actor);
        }

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
