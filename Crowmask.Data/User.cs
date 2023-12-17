using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; }

        public string FullName { get; set; }

        public string ProfileText { get; set; }

        public string Url { get; set; }

        public string IconUrl { get; set; }

        public int? Age { get; set; }

        public string Gender { get; set; }

        public string Location { get; set; }

        public IEnumerable<UserLink> Links { get; set; }
    }
}
