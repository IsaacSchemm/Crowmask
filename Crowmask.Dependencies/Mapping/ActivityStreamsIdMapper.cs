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
        /// <param name="identifier">The submission or journal ID</param>
        public string GetObjectId(JointIdentifier identifier) =>
            identifier.IsSubmissionIdentifier
                ? $"https://{crowmaskHost.Hostname}/api/submissions/{identifier.submitid}"
            : identifier.IsJournalIdentifier
                ? $"https://{crowmaskHost.Hostname}/api/journals/{identifier.journalid}"
            : throw new NotImplementedException();

        /// <summary>
        /// Determines the ID to use for a Create activity for a post.
        /// </summary>
        /// <param name="identifier">The submission or journal ID</param>
        public string GetCreateId(JointIdentifier identifier) =>
            $"{GetObjectId(identifier)}#create";

        /// <summary>
        /// Extracts the submission or journal ID, if any, from an ActivityPub
        /// object ID.
        /// </summary>
        /// <param name="objectId">The ActivityPub ID / URL for a Crowmask post</param>
        /// <returns>A submission or journal ID (in a JointIdentifier union), or null</returns>
        public JointIdentifier? GetJointIdentifier(string objectId)
        {
            if (!Uri.TryCreate(objectId, UriKind.Absolute, out Uri? uri))
                return null;

            if (!int.TryParse(uri.AbsolutePath.Split('/').Last(), out int id))
                return null;

            foreach (var candidate in new[]
            {
                JointIdentifier.NewSubmissionIdentifier(id),
                JointIdentifier.NewJournalIdentifier(id)
            })
            {
                if (GetObjectId(candidate) == objectId)
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Determines the URL for a notification post sent to the admin actor.
        /// </summary>
        /// <param name="identifier">The submission or journal ID</param>
        /// <param name="interaction">The interaction (like, boost, or reply) object</param>
        /// <returns>An ActivityPub object ID</returns>
        public string GetObjectId(JointIdentifier identifier, Interaction interaction) =>
            $"{GetObjectId(identifier)}/interactions/{interaction.Id}/notification";
    }
}
