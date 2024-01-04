#nullable disable

using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    public class SubmissionBoost
    {
        public Guid Id { get; set; }

        public DateTimeOffset AddedAt { get; set; }

        [Required]
        public string ActivityId { get; set; }

        [Required]
        public string ActorId { get; set; }
    }
}
