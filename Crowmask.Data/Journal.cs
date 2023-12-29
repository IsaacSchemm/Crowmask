using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    public class Journal
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int JournalId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTimeOffset PostedAt { get; set; }

        [Required]
        public string Rating { get; set; }

        public bool VisibilityRestricted { get; set; }

        public DateTimeOffset FirstCachedAt { get; set; }

        public DateTimeOffset CacheRefreshAttemptedAt { get; set; }

        public DateTimeOffset CacheRefreshSucceededAt { get; set; }

        public string Link => $"https://www.weasyl.com/journal/{JournalId}";

        public bool Stale
        {
            get
            {
                var now = DateTimeOffset.UtcNow;

                bool older_than_1_hour =
                    now - PostedAt > TimeSpan.FromHours(1);
                bool older_than_7_days =
                    now - PostedAt > TimeSpan.FromDays(7);
                bool older_than_28_days =
                    now - PostedAt > TimeSpan.FromDays(28);

                bool refreshed_within_1_hour =
                    now - CacheRefreshSucceededAt < TimeSpan.FromHours(1);
                bool refreshed_within_7_days =
                    now - CacheRefreshSucceededAt < TimeSpan.FromDays(7);
                bool refreshed_within_28_days =
                    now - CacheRefreshSucceededAt < TimeSpan.FromDays(28);

                bool refresh_attempted_within_5_minutes =
                    now - CacheRefreshAttemptedAt < TimeSpan.FromMinutes(5);

                if (refresh_attempted_within_5_minutes)
                    return false;

                if (older_than_1_hour && refreshed_within_1_hour)
                    return false;

                if (older_than_7_days && refreshed_within_7_days)
                    return false;

                if (older_than_28_days && refreshed_within_28_days)
                    return false;

                return true;
            }
        }
    }
}
