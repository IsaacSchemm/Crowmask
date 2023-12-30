using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    public class UserLink
    {
        public Guid Id { get; set; }

        [Required]
        public string Site { get; set; }

        [Required]
        public string UsernameOrUrl { get; set; }

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
