using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CrosspostSharp3.Weasyl;
using System.Collections.Generic;
using Crowmask.ActivityPub;
using System.Linq;

namespace Crowmask.Functions
{
    public class Actor(WeasylClient weasylClient)
    {
        [FunctionName("Actor")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            ILogger log)
        {
            var self = await weasylClient.WhoamiAsync();
            var user = await weasylClient.GetUserAsync(self.login);

            IEnumerable<object> getAttachments()
            {
                if (user.user_info.gender is string g)
                {
                    yield return new
                    {
                        type = "PropertyValue",
                        name = "Gender",
                        value = g
                    };
                }
                foreach (var link in user.user_info.user_links)
                {
                    yield return new
                    {
                        type = "PropertyValue",
                        name = link.Key,
                        value = link.Value.First()
                    };
                }
            }

            return new JsonResult(new Dictionary<string, object>
            {
                ["@context"] = new[] {
                    "https://www.w3.org/ns/activitystreams",
                    "https://w3id.org/security/v1"
                },
                ["id"] = $"https://{AP.HOST}/api/actor",
                ["type"] = "Person",
                ["inbox"] = $"https://{AP.HOST}/api/actor/inbox",
                ["outbox"] = $"https://{AP.HOST}/api/actor/outbox",
                ["followers"] = $"https://{AP.HOST}/api/actor/followers",
                ["following"] = $"https://{AP.HOST}/api/actor/following",
                ["preferredUsername"] = user.username,
                ["name"] = user.full_name,
                ["summary"] = user.profile_text,
                ["url"] = user.link,
                ["publicKey"] = new
                {
                    id = "https://crowmask20231213.azurewebsites.net/api/actor#main-key",
                    owner = "https://crowmask20231213.azurewebsites.net/api/actor",
                    publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoHfLR9OTkg8mMvziXlrt8uQqWH3u13RJSlCN1w0TE7R0WvG4w1SEL+QWQY61X+STRJ/emzPX3fi6X/FTapLrMdVg4CHio3VW5Jr8qvgG56NfJ5QCxDsB+VzLiCWVp7Dge2v6WGgitfndNhMu/nvUMRft8a+Q7QWqNQ9iNCVBS1KRm2WEVs0hUvfCubQtv0DzUFTmnFi1sjHG/G1kwlukp/V+fLqGQzBjkrdQ0vvorRZwKvnTjdqRNjgq9580x+tEHfnCX4DScnwu/jWEMD9VmpZfE4/UD91yQMCihqv/NvAU0EVdgnH1hI2xWDhCeQ1zEKCS/bCcHxT30SLfsMI2PQIDAQAB\n-----END PUBLIC KEY-----"
                },
                ["icon"] = new
                {
                    mediaType = "image/png",
                    type = "Image",
                    url = user.media.avatar.First().url
                },
                ["attachment"] = getAttachments()
                //["attachment"] = new[]
                //{
                //    new
                //    {
                //        type = "PropertyValue",
                //        name = "x",
                //        value = "y"
                //    }
                //}
            });
        }
    }
}
