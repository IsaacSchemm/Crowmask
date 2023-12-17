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

        public DateTimeOffset PostedAt { get; set; }

        public IEnumerable<SubmissionMedia> Media { get; set; }

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

        public IEnumerable<SubmissionTag> Tags { get; set; }

        public string Title { get; set; }

        public DateTimeOffset CacheRefreshAttemptedAt { get; set; }

        public DateTimeOffset CacheRefreshSucceededAt { get; set; }
    }
}
