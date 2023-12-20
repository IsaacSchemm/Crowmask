using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class UserAvatar
    {
        public Guid Id { get; set; }

        [Required]
        public string Url { get; set; }

        public int? MediaId { get; set; }
    }
}
