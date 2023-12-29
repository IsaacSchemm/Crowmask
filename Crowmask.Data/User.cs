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

        public bool Stale
        {
            get
            {
                var now = DateTimeOffset.UtcNow;

                bool refreshed_within_1_hour =
                    now - CacheRefreshSucceededAt < TimeSpan.FromHours(1);

                bool refresh_attempted_within_4_minutes =
                    now - CacheRefreshAttemptedAt < TimeSpan.FromMinutes(4);

                if (refresh_attempted_within_4_minutes)
                    return false;

                if (refreshed_within_1_hour)
                    return false;

                return true;
            }
        }
    }
}
