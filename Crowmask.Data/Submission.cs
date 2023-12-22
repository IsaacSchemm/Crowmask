using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    public class Submission
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SubmitId { get; set; }

        public int UserId { get; set; }

        public string Description { get; set; }

        public bool FriendsOnly { get; set; }

        public string Link { get; set; }

        public IEnumerable<SubmissionMedia> Media { get; set; } = new List<SubmissionMedia>(0);

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

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public string Content => Description ?? "";

        public string Url => Link ?? $"https://www.weasyl.com/~lizardsocks/submissions/{SubmitId}";
    }
}
