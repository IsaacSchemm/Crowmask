using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class OutboundActivity
    {
        public Guid Id { get; set; }

        [Required]
        public string Inbox { get; set; }

        [Required]
        public string JsonBody { get; set; }

        public DateTimeOffset PublishedAt { get; set; }

        public bool Sent { get; set; }

        public long Failures { get; set; }
    }
}
