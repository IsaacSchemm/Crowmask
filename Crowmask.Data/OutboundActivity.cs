using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class OutboundActivity
    {
        public long Id { get; set; }

        [Required]
        public string Inbox { get; set; }

        [Required]
        public string JsonBody { get; set; }
    }
}
