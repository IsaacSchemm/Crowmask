using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    /// <summary>
    /// A tag associated with a particular submission on Weasyl.
    /// </summary>
    public class SubmissionTag
    {
        /// <summary>
        /// An internal Crowmask ID, for compatibility with relational
        /// database backends.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The tag string from the Weasyl API.
        /// </summary>
        [Required]
        public string Tag { get; set; } = "";
    }
}
