using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int InternalUserId { get; set; }

        [Required]
        public string Username { get; set; }

        public string FullName { get; set; }

        public string ProfileText { get; set; }

        [Required]
        public string Url { get; set; }

        public IEnumerable<UserAvatar> Avatars { get; set; } = new List<UserAvatar>(0);

        public int? Age { get; set; }

        public string Gender { get; set; }

        public string Location { get; set; }

        public IEnumerable<UserLink> Links { get; set; } = new List<UserLink>(0);

        public DateTimeOffset CacheRefreshAttemptedAt { get; set; }

        public DateTimeOffset CacheRefreshSucceededAt { get; set; }

        [NotMapped]
        public string DisplayName => FullName ?? Username;

        [NotMapped]
        public string Summary => ProfileText ?? "";
    }
}
