#nullable disable

using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    /// <summary>
    /// A record of another ActivityPub user's "like" of this journal entry.
    /// </summary>
    public class JournalLike
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
        /// The Like activity's ID.
        /// </summary>
        [Required]
        public string ActivityId { get; set; }

        /// <summary>
        /// The ID of the actor who published the Like activity.
        /// </summary>
        [Required]
        public string ActorId { get; set; }
    }
}
