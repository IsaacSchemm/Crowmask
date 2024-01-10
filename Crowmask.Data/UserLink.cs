using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    /// <summary>
    /// A pointer from a Weasyl user to another social media site (with or
    /// without a working URL).
    /// </summary>
    public class UserLink
    {
        /// <summary>
        /// An internal Crowmask ID, for compatibility with relational
        /// database backends.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the site this link is for.
        /// </summary>
        [Required]
        public string Site { get; set; }

        /// <summary>
        /// The username / URL value entered by the user on their Weasyl profile.
        /// </summary>
        [Required]
        public string UsernameOrUrl { get; set; }

        /// <summary>
        /// A valid URL for the hyperlink, or null if Crowmask cannot
        /// determine the intended URL.
        /// </summary>
        [NotMapped]
        public string Url
        {
            get
            {
                if (Uri.TryCreate(UsernameOrUrl, UriKind.Absolute, out Uri direct))
                {
                    return direct.Scheme switch
                    {
                        "http" or "https" or "mailto" => direct.AbsoluteUri,
                        _ => null,
                    };
                }
                else
                {
                    string enc = Uri.EscapeDataString(UsernameOrUrl);
                    return Site switch
                    {
                        "DeviantArt" => $"https://www.deviantart.com/{enc}",
                        "Facebook" => $"https://www.facebook.com/{enc}",
                        "Flickr" => $"https://www.flickr.com/photos/{enc}",
                        "Fur Affinity" => $"https://www.furaffinity.net/user/{enc}",
                        "Inkbunny" => $"https://inkbunny.net/{enc}",
                        "reddit" => $"https://www.reddit.com/user/{enc}",
                        "SoFurry" => $"https://{enc}.sofurry.com/",
                        "Steam" => $"https://steamcommunity.com/id/{enc}",
                        "Tumblr" => $"https://{enc}.tumblr.com/",
                        "Twitter" => $"https://twitter.com/{enc}",
                        "YouTube" => $"https://www.youtube.com/user/{enc}",
                        "Patreon" => $"https://www.patreon.com/{enc}",
                        _ => null,
                    };
                }
            }
        }
    }
}
