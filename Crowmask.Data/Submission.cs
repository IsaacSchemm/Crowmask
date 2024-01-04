using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    public class Submission
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SubmitId { get; set; }

        public string Description { get; set; }

        public bool FriendsOnly { get; set; }

        public string Link { get; set; }

        public List<SubmissionMedia> Media { get; set; } = [];

        public List<SubmissionThumbnail> Thumbnails { get; set; } = [];

        public DateTimeOffset PostedAt { get; set; }

        public enum Rating
        {
            General = 1,
            Moderate = 2,
            Mature = 3,
            Explicit = 4
        }

        public Rating RatingId { get; set; }

        public enum Subtype
        {
            Visual = 1,
            Literary = 2,
            Multimedia = 3
        }

        public Subtype SubtypeId { get; set; }

        public List<SubmissionTag> Tags { get; set; } = [];

        public string Title { get; set; }

        public DateTimeOffset FirstCachedAt { get; set; }

        public DateTimeOffset CacheRefreshAttemptedAt { get; set; }

        public DateTimeOffset CacheRefreshSucceededAt { get; set; }

        public List<SubmissionBoost> Boosts { get; set; } = [];

        public List<SubmissionLike> Likes { get; set; } = [];

        public List<SubmissionReply> Replies { get; set; } = [];

        public string Content => Description ?? "";

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

                bool refresh_attempted_within_4_minutes =
                    now - CacheRefreshAttemptedAt < TimeSpan.FromMinutes(4);

                if (refresh_attempted_within_4_minutes)
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
