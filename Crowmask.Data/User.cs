using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    /// <summary>
    /// Represents the sole Weasyl user and the sole ActivityPub actor exposed
    /// by Crowmask, and contains cached data pulled from the user's Weasyl
    /// profile.
    /// </summary>
    public class User
    {
        /// <summary>
        /// An internal user ID. Should always be set to 0; included for
        /// compatibility with relational database backends.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int InternalUserId { get; set; }

        /// <summary>
        /// The user's Weasyl username, which will also be used in their ActivityPub username.
        /// </summary>
        [Required]
        public string Username { get; set; }

        /// <summary>
        /// The "Full Name" fromm the user's Weasyl profile.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// HTML content pulled from Weasyl's "Profile Text" field.
        /// </summary>
        public string ProfileText { get; set; }

        /// <summary>
        /// The Weasyl profile URL.
        /// </summary>
        [Required]
        public string Url { get; set; }

        /// <summary>
        /// A list of the user's avatars from the Weasyl API.
        /// </summary>
        public IEnumerable<UserAvatar> Avatars { get; set; } = new List<UserAvatar>(0);

        /// <summary>
        /// The user's age, if displayed in their profile.
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// The user's gender, as entered in their profile (if present).
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// The user's location, as entered in their profile (if present).
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Links to other social media, pulled from the "Contact and Social
        /// Media" section of the Weasyl profile.
        /// </summary>
        public IEnumerable<UserLink> Links { get; set; } = new List<UserLink>(0);

        /// <summary>
        /// The display name to use over ActivityPub.
        /// </summary>
        [NotMapped]
        public string DisplayName => FullName ?? Username;

        /// <summary>
        /// The HTML text to use in the ActivityPub summary field.
        /// </summary>
        [NotMapped]
        public string Summary => ProfileText ?? "";
    }
}
