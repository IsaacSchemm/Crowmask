using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    /// <summary>
    /// Holds access tokens and refresh tokens for atproto sessions that
    /// correspond to bot accounts Crowmask is configured to post to.
    /// </summary>
    public class ATProtoSession
    {
        /// <summary>
        /// The user's DID.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string DID { get; set; }

        /// <summary>
        /// An access token.
        /// </summary>
        [Required]
        public string AccessToken { get; set; }

        /// <summary>
        /// A refresh token.
        /// </summary>
        [Required]
        public string RefreshToken { get; set; }

        /// <summary>
        /// The CID of the most recent notification Crowmask has seen from the
        /// atproto account (if any).
        /// </summary>
        public string LastSeenCid { get; set; }
    }
}
