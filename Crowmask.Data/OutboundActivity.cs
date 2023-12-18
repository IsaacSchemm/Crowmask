using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class OutboundActivity
    {
        public long Id { get; set; }

        public Guid ExternalId { get; set; }

        [Required]
        public string Inbox { get; set; }

        [Required]
        public string JsonBody { get; set; }

        public DateTimeOffset StoredAt { get; set; }

        public DateTimeOffset DelayUntil { get; set; }

        public bool Sent { get; set; }
    }
}
