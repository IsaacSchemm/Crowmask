using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Crowmask.Functions
{
    public static class Actor
    {
        [FunctionName("Actor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            ILogger log)
        {
            return new ContentResult
            {
                StatusCode = 200,
                Content = @"{
  ""@context"": [
      ""https://www.w3.org/ns/activitystreams"",
      ""https://w3id.org/security/v1""
    ],
  ""id"": ""https://crowmask20231213.azurewebsites.net/api/actor"",
  ""type"": ""Person"",
  ""preferredUsername"": ""xyz"",
  ""inbox"": ""https://crowmask20231213.azurewebsites.net/api/actor/inbox"",
  ""outbox"": ""https://crowmask20231213.azurewebsites.net/api/actor/outbox"",
  ""followers"": ""https://crowmask20231213.azurewebsites.net/api/actor/followers"",
  ""following"": ""https://crowmask20231213.azurewebsites.net/api/actor/following"",
  ""publicKey"": {
    ""id"": ""https://crowmask20231213.azurewebsites.net/api/actor#main-key"",
    ""owner"": ""https://crowmask20231213.azurewebsites.net/api/actor"",
    ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoHfLR9OTkg8mMvziXlrt8uQqWH3u13RJSlCN1w0TE7R0WvG4w1SEL+QWQY61X+STRJ/emzPX3fi6X/FTapLrMdVg4CHio3VW5Jr8qvgG56NfJ5QCxDsB+VzLiCWVp7Dge2v6WGgitfndNhMu/nvUMRft8a+Q7QWqNQ9iNCVBS1KRm2WEVs0hUvfCubQtv0DzUFTmnFi1sjHG/G1kwlukp/V+fLqGQzBjkrdQ0vvorRZwKvnTjdqRNjgq9580x+tEHfnCX4DScnwu/jWEMD9VmpZfE4/UD91yQMCihqv/NvAU0EVdgnH1hI2xWDhCeQ1zEKCS/bCcHxT30SLfsMI2PQIDAQAB\n-----END PUBLIC KEY-----""
  }
}",
                ContentType = "application/activity+json"
            };
        }
    }
}
