using Crowmask.ActivityPub;
using Crowmask.Cache;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Creations(CrowmaskCache cache, Translator translator)
    {
        [Function("OutboxObjects")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/creations/{submitid}")] HttpRequestData req,
            int submitid)
        {
            var submission = await cache.GetSubmission(submitid);

            return submission == null
                ? req.CreateResponse(HttpStatusCode.NotFound)
                : await req.WriteCrowmaskResponseAsync(
                    Markdown.ContentNegotiation.CrowmaskFormat.ActivityJson,
                    AP.SerializeWithContext(translator.ObjectToCreate(submission)));
        }
    }
}
