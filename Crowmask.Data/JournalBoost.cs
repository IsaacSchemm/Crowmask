#nullable disable

using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    /// <summary>
    /// A boost (Announce activity) made by another user to share this journal
    /// entry with their followers.
    /// </summary>
    public class JournalBoost
    {
        /// <summary>
        /// The Crowmask internal ID for this interaction.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The date/time when this interaction was recieved by Crowmask.
        /// </summary>
        public DateTimeOffset AddedAt { get; set; }

        /// <summary>
        /// The Announce activity's ID.
        /// </summary>
        [Required]
        public string ActivityId { get; set; }

        /// <summary>
        /// The ID of the actor who published the Announce activity.
        /// </summary>
        [Required]
        public string ActorId { get; set; }
    }
}
