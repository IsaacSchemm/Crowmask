using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class KnownInbox
    {
        public Guid Id { get; set; }

        [Required]
        public string Inbox { get; set; }
    }
}
