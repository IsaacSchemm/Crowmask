using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Markdown;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class SubmissionsLikes(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, Translator translator)
    {
        [Function("SubmissionsLikes")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/submissions/{submitid}/likes")] HttpRequestData req,
            int submitid)
        {
            var cacheResult = await crowmaskCache.GetSubmissionAsync(submitid);

            if (cacheResult.AsList.IsEmpty)
                return req.CreateResponse(HttpStatusCode.NotFound);

            var submission = cacheResult.AsList.Head;

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
                    return await req.WriteCrowmaskResponseAsync(format, AP.SerializeWithContext(translator.AsLikesCollection(submission)));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
