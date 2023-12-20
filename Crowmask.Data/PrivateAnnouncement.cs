using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class PrivateAnnouncement
    {
        public Guid Id { get; set; }

        [Required]
        public string AnnouncedObjectId { get; set; }

        public DateTimeOffset PublishedAt { get; set; }

        public bool Sent { get; set; }
    }
}
