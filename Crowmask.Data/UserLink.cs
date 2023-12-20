using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class UserLink
    {
        public Guid Id { get; set; }

        [Required]
        public string Site { get; set; }

        [Required]
        public string UsernameOrUrl { get; set; }
    }
}
