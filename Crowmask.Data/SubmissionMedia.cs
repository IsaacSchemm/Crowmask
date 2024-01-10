using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    /// <summary>
    /// Media (an image or another item) attached to a Weasyl submission.
    /// </summary>
    public class SubmissionMedia
    {
        /// <summary>
        /// An internal Crowmask ID, for compatibility with relational
        /// database backends.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The Weasyl-hosted URL of the image or other item.
        /// </summary>
        [Required]
        public string Url { get; set; } = "";

        /// <summary>
        /// The media type of the URL, detected by Crowmask with a HEAD request.
        /// </summary>
        [Required]
        public string ContentType { get; set; } = "";
    }
}
