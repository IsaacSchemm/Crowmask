using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Markdown;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class JournalsComments(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, Translator translator)
    {
        [Function("JournalsComments")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/journals/{journalid}/comments")] HttpRequestData req,
            int journalid)
        {
            var cacheResult = await crowmaskCache.GetJournalAsync(journalid);

            if (cacheResult.AsList.IsEmpty)
                return req.CreateResponse(HttpStatusCode.NotFound);

            var journal = cacheResult.AsList.Head;

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
                    return await req.WriteCrowmaskResponseAsync(format, AP.SerializeWithContext(translator.AsCommentsCollection(journal)));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
