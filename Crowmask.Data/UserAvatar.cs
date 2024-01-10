using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    /// <summary>
    /// A Weasyl avatar image associated with a user.
    /// </summary>
    public class UserAvatar
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
        public string Url { get; set; }
    }
}
