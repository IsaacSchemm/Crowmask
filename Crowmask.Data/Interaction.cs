using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    /// <summary>
    /// An interaction by a remote actor with a Crowmask post.
    /// </summary>
    public class Interaction
    {
        /// <summary>
        /// The Crowmask internal ID for this activity. Used to construct the object ID of the notification.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The date/time when this activity was recieved by Crowmask.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }

        /// <summary>
        /// The ID of the actor who created the activity.
        /// </summary>
        [Required]
        public string ActorId { get; set; }

        /// <summary>
        /// The ActivityPub type of the activity (e.g. https://www.w3.org/ns/activitystreams#Like).
        /// </summary>
        [Required]
        public string ActivityType { get; set; }

        /// <summary>
        /// The ActivityPub ID of the activity.
        /// </summary>
        [Required]
        public string ActivityId { get; set; }

        /// <summary>
        /// The ActivityPub ID of the Crowmask object the activity is in reference to.
        /// Can be a post ID or the actor ID.
        /// </summary>
        [Required]
        public string TargetId { get; set; }
    }
}
