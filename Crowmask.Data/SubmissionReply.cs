#nullable disable

using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    /// <summary>
    /// A record of another ActivityPub user's reply to this submission.
    /// </summary>
    public class SubmissionReply
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
        /// The ID of the external post that was made in reply to this one.
        /// </summary>
        [Required]
        public string ObjectId { get; set; }

        /// <summary>
        /// The ID of the actor who published the reply.
        /// </summary>
        [Required]
        public string ActorId { get; set; }
    }
}
