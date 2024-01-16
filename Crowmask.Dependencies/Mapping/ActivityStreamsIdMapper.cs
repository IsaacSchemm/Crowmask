using Crowmask.DomainModeling;
using Crowmask.Interfaces;

namespace Crowmask.Dependencies.Mapping
{
    /// <summary>
    /// Provides mappings between Crowmask's internal IDs and the public
    /// ActivityPub IDs of corresponding objects.
    /// </summary>
    public class ActivityStreamsIdMapper(ICrowmaskHost crowmaskHost)
    {
        /// <summary>
        /// The ActivityPub actor ID of the single actor hosted by this Crowmask instance.
        /// </summary>
        public string ActorId =>
            $"https://{crowmaskHost.Hostname}/api/actor";

        /// <summary>
        /// Generates a random ID that is not intended to be looked up.
        /// Used for Update and Delete activities.
        /// </summary>
        public string GetTransientId() =>
            $"https://{crowmaskHost.Hostname}#transient-{Guid.NewGuid()}";

        /// <summary>
        /// Determines the ActivityPub object ID for a post.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        public string GetObjectId(int submitid) =>
            $"https://{crowmaskHost.Hostname}/api/submissions/{submitid}";

        /// <summary>
        /// Determines the ID to use for a Create activity for a post.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        public string GetCreateId(int submitid) =>
            $"{GetObjectId(submitid)}#create";

        /// <summary>
        /// Extracts the submission ID, if any, from an ActivityPub object ID.
        /// </summary>
        /// <param name="objectId">The ActivityPub ID / URL for a Crowmask post</param>
        /// <returns>A submission ID, or null</returns>
        public int? GetSubmitId(string objectId)
        {
            return Uri.TryCreate(objectId, UriKind.Absolute, out Uri? uri)
                && int.TryParse(uri.AbsolutePath.Split('/').Last(), out int candidate)
                && GetObjectId(candidate) == objectId
                    ? candidate
                    : null;
        }

        /// <summary>
        /// Determines the URL for a notification post sent to the admin actor.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <param name="interaction">The interaction (like, boost, or reply) object</param>
        /// <returns>An ActivityPub object ID</returns>
        public string GetObjectId(int submitid, Interaction interaction) =>
            $"{GetObjectId(submitid)}/interactions/{interaction.Id}/notification";

        /// <summary>
        /// Determines the URL for a notification post sent to the admin actor.
        /// </summary>
        /// <param name="remotePost">The remote mention</param>
        public string GetObjectId(RemotePost remotePost) =>
            $"https://{crowmaskHost.Hostname}/api/mentions/{remotePost.id}/notification";
    }
}
