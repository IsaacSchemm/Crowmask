using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    /// <summary>
    /// A remote post in which this Crowmask actor is mentioned.
    /// </summary>
    public class Mention
    {
        /// <summary>
        /// The Crowmask internal ID for this mention. Used to construct the object ID of the notification.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The date/time when this mention was recieved by Crowmask.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }

        /// <summary>
        /// The ID of the actor who created this mention.
        /// </summary>
        [Required]
        public string ActorId { get; set; }

        /// <summary>
        /// The ActivityPub ID of the mention (Note, etc.)
        /// </summary>
        [Required]
        public string ObjectId { get; set; }

        /// <summary>
        /// The date/time (if any) when administrator of this Crowmask
        /// instance dismissed this notification via the API.
        /// </summary>
        public DateTimeOffset? DismissedAt { get; set; }
    }
}
