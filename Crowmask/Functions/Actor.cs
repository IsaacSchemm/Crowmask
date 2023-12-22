using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Remote;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(CrowmaskCache crowmaskCache, KeyProvider keyProvider, Translator translator)
    {
        [FunctionName("Actor")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor")] HttpRequest req,
            ILogger log)
        {
            var person = await crowmaskCache.GetUser();

            var key = await keyProvider.GetPublicKeyAsync();

            string json = AP.SerializeWithContext(translator.PersonToObject(person, key));

            return new ContentResult
            {
                Content = json,
                ContentType = "application/json"
            };
        }
    }
}
