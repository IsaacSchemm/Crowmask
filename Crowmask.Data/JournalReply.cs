#nullable disable

using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    public class JournalReply
    {
        public Guid Id { get; set; }

        public DateTimeOffset AddedAt { get; set; }

        [Required]
        public string ObjectId { get; set; }

        [Required]
        public string ActorId { get; set; }
    }
}
