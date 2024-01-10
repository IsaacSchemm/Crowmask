using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    /// <summary>
    /// A thumbnail attached to a Weasyl submission, used in the HTML view of
    /// the outbox.
    /// </summary>
    public class SubmissionThumbnail
    {
        /// <summary>
        /// An internal Crowmask ID, for compatibility with relational
        /// database backends.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The Weasyl-hosted URL of the image.
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
