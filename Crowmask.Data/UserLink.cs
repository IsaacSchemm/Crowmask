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
        public Uri Uri
        {
            get
            {
                if (Uri.TryCreate(UsernameOrUrl, UriKind.Absolute, out Uri direct))
                {
                    return direct.Scheme switch
                    {
                        "http" or "https" or "mailto" => direct,
                        _ => null,
                    };
                }
                else
                {
                    string enc = Uri.EscapeDataString(UsernameOrUrl);
                    return Site switch
                    {
                        "DeviantArt" => new Uri($"https://www.deviantart.com/{enc}"),
                        "Facebook" => new Uri($"https://www.facebook.com/{enc}"),
                        "Flickr" => new Uri($"https://www.flickr.com/photos/{enc}"),
                        "Fur Affinity" => new Uri($"https://www.furaffinity.net/user/{enc}"),
                        "Inkbunny" => new Uri($"https://inkbunny.net/{enc}"),
                        "reddit" => new Uri($"https://www.reddit.com/user/{enc}"),
                        "SoFurry" => new Uri($"https://{enc}.sofurry.com/"),
                        "Steam" => new Uri($"https://steamcommunity.com/id/{enc}"),
                        "Tumblr" => new Uri($"https://{enc}.tumblr.com/"),
                        "Twitter" => new Uri($"https://twitter.com/{enc}"),
                        "YouTube" => new Uri($"https://www.youtube.com/user/{enc}"),
                        "Patreon" => new Uri($"https://www.patreon.com/{enc}"),
                        _ => null,
                    };
                }
            }
        }
    }
}
