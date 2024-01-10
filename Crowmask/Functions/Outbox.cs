using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Markdown;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Outbox(CrowmaskCache crowmaskCache, Translator translator, MarkdownTranslator markdownTranslator)
    {
        /// <summary>
        /// Returns the size of the user's outbox (which contains artwork
        /// submissions and journals) and a link to the first page.
        /// </summary>
        /// <param name="req"></param>
        /// <returns>An ActivityStreams OrderedCollection or a Markdown or HTML response, depending on the user agent's Accept header.</returns>
        [Function("Outbox")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox")] HttpRequestData req)
        {
            int count = await crowmaskCache.GetCachedPostCountAsync();

            var gallery = Domain.AsGallery(count: count);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityStreams)
                {
                    var outbox = translator.AsOutbox(gallery);

                    string json = AP.SerializeWithContext(outbox);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(gallery));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(gallery));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
