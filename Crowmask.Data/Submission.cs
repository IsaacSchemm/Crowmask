using Crowmask.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    /// <summary>
    /// A Weasyl artwork submission that has been cached in Crowmask.
    /// </summary>
    public class Submission : IPerishable
    {
        /// <summary>
        /// The submission ID on Weasyl, which is also used as an internal ID by Crowmask.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SubmitId { get; set; }

        /// <summary>
        /// The HTML description of the artwork.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The URL to the submission on Weasyl.
        /// </summary>
        [Required]
        public string Link { get; set; }

        /// <summary>
        /// Media (an image or another item) attached to a Weasyl submission.
        /// </summary>
        public class SubmissionMedia
        {
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

        /// <summary>
        /// A list of media (i.e. images) associated with the submission.
        /// </summary>
        public List<SubmissionMedia> Media { get; set; } = [];

        /// <summary>
        /// A list of thumbnails associated with the submission.
        /// </summary>
        public List<SubmissionMedia> Thumbnails { get; set; } = [];

        /// <summary>
        /// When the submission was posted to Weasyl.
        /// </summary>
        public DateTimeOffset PostedAt { get; set; }

        /// <summary>
        /// The rating of this submission on Weasyl. 
        /// Crowmask will mark anything other than "general" as sensitive on the ActivityPub side.
        /// </summary>
        public string Rating { get; set; }

        /// <summary>
        /// The submission type (visual, literary, etc.)
        /// </summary>
        public string Subtype { get; set; }

        /// <summary>
        /// A tag associated with a particular submission on Weasyl.
        /// </summary>
        public class SubmissionTag
        {
            /// <summary>
            /// The tag string from the Weasyl API.
            /// </summary>
            [Required]
            public string Tag { get; set; } = "";
        }

        /// <summary>
        /// A list of tags associated with the submission on Weasyl.
        /// </summary>
        public List<SubmissionTag> Tags { get; set; } = [];

        /// <summary>
        /// The title of the submission on Weasyl.
        /// </summary>
        public string Title { get; set; }

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
        /// The HTML content to use in ActivityPub for this post.
        /// </summary>
        public string Content => Description ?? "";

        /// <summary>
        /// Whether this is considered an artwork submission.
        /// </summary>
        public bool Visual => Subtype == "visual" || Subtype == null;
    }
}
