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

        public IEnumerable<SubmissionMedia> Media { get; set; } = new List<SubmissionMedia>(0);

        public IEnumerable<SubmissionThumbnail> Thumbnails { get; set; } = new List<SubmissionThumbnail>(0);

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

        public IEnumerable<SubmissionTag> Tags { get; set; } = new List<SubmissionTag>(0);

        public string Title { get; set; }

        public DateTimeOffset FirstCachedAt { get; set; }

        public DateTimeOffset CacheRefreshAttemptedAt { get; set; }

        public DateTimeOffset CacheRefreshSucceededAt { get; set; }

        public string Content => Description ?? "";

        public string Url => Link ?? $"https://www.weasyl.com/~lizardsocks/submissions/{SubmitId}";

        public bool Stale
        {
            get
            {
                if (!Thumbnails.Any() && CacheRefreshSucceededAt < DateTimeOffset.FromUnixTimeMilliseconds(1703806072207L))
                    return true;

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
