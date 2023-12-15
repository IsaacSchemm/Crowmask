using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    public class Submission
    {
        [Key]
        public Guid Id { get; set; }

        public int SubmitId { get; set; }

        public string? Description { get; set; }

        public bool FriendsOnly { get; set; }

        public DateTimeOffset PostedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

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

        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();

        public string? Title { get; set; }

        public IEnumerable<string> Urls { get; set; } = Enumerable.Empty<string>();
    }
}
