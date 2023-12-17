using Crowmask.ActivityPub;
using Crowmask.Cache;
using JsonLD.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(CrowmaskCache crowmaskCache, IPublicKey key)
    {
        [FunctionName("Actor")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "api/actor")] HttpRequest req,
            ILogger log)
        {
            var person = await crowmaskCache.GetUser();

            string json = AP.Serialize(AP.PersonToObject(person, key));

            var document = JObject.Parse(json);
            var expanded = JsonLdProcessor.Expand(document);
            Console.WriteLine(expanded);

            return new JsonResult(json);
        }
    }
}
