using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Crowmask.Interfaces;

#nullable disable

namespace Crowmask.Data
{
    public class Journal : IPerishable
    {
        /// <summary>
        /// The journal ID on Weasyl, which is also used as an internal ID by Crowmask.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int JournalId { get; set; }

        /// <summary>
        /// The HTML content of the journal entry.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The URL to the submission on Weasyl.
        /// </summary>
        [Required]
        public string Link { get; set; }

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
        /// A tag associated with a particular submission on Weasyl.
        /// </summary>
        public class JournalTag
        {
            /// <summary>
            /// The tag string from the Weasyl API.
            /// </summary>
            [Required]
            public string Tag { get; set; }
        }

        /// <summary>
        /// A list of tags associated with the submission on Weasyl.
        /// </summary>
        public List<JournalTag> Tags { get; set; } = [];

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
    }
}
