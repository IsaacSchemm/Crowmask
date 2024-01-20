using Crowmask.Formats;
using Crowmask.Library;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Outbox(
        SubmissionCache cache,
        ActivityPubTranslator translator,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator)
    {
        /// <summary>
        /// Returns the size of the user's outbox and a link to the first page.
        /// </summary>
        /// <param name="req"></param>
        /// <returns>An ActivityStreams OrderedCollection or a Markdown or HTML response, depending on the user agent's Accept header.</returns>
        [Function("Outbox")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox")] HttpRequestData req)
        {
            int count = await cache.GetCachedSubmissionCountAsync();

            var gallery = Domain.AsGallery(count: count);

            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    var outbox = translator.AsOutbox(gallery);

                    string json = ActivityPubSerializer.SerializeWithContext(outbox);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.Family.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(gallery));
                }
                else if (format.Family.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(gallery));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
