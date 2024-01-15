using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    /// <summary>
    /// A Weasyl journal entry that has been cached in Crowmask.
    /// </summary>
    public class Journal
    {
        /// <summary>
        /// The journal ID on Weasyl, which is also used as an internal ID by Crowmask.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int JournalId { get; set; }

        /// <summary>
        /// The title of the journal entry.
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// The HTML content of the journal entry.
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// When the journal entry was posted.
        /// </summary>
        public DateTimeOffset PostedAt { get; set; }

        /// <summary>
        /// The rating of this journal entry on Weasyl.
        /// Crowmask will mark anything other than "general" as sensitive on the ActivityPub side.
        /// </summary>
        public string Rating { get; set; }

        /// <summary>
        /// The date/time when Crowmask first added this object to its cache.
        /// </summary>
        public DateTimeOffset FirstCachedAt { get; set; }

        /// <summary>
        /// The most recent date/time when Crowmask tried to update this
        /// object.
        /// </summary>
        public DateTimeOffset CacheRefreshAttemptedAt { get; set; }

        /// <summary>
        /// The most recent date/time when Crowmask successfully updated this
        /// object. (It may not have changed.)
        /// </summary>
        public DateTimeOffset CacheRefreshSucceededAt { get; set; }

        /// <summary>
        /// A list of boosts (Announce activities) recieved for this object.
        /// </summary>
        public List<JournalBoost> Boosts { get; set; } = [];

        /// <summary>
        /// A list of likes (Like activities) recieved for this object.
        /// </summary>
        public List<JournalLike> Likes { get; set; } = [];

        /// <summary>
        /// A list of ActivityPub posts made in reply to this object.
        /// </summary>
        public List<JournalReply> Replies { get; set; } = [];

        /// <summary>
        /// The URL to the journal entry on Weasyl.
        /// </summary>
        public string Link => $"https://www.weasyl.com/journal/{JournalId}";

        /// <summary>
        /// Whether Crowmask considers the cached journal entry "stale".
        /// This is used in CrowmaskCache to decide whether to call out to
        /// Weasyl and re-fetch the information.
        /// </summary>
        public bool Stale
        {
            get
            {
                if (Rating == null)
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
