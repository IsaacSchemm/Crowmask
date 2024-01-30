using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Submissions(
        SubmissionCache cache,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator,
        ActivityPubTranslator translator)
    {
        /// <summary>
        /// Returns a mirror of an artwork submission posted to Weasyl.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="submitid">The numeric ID of the submission on Weasyl</param>
        /// <returns>An ActivityStreams Note or Collection or a Markdown or HTML response, depending on the query string and the user agent's Accept header.</returns>
        [Function("Submissions")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/submissions/{submitid}")] HttpRequestData req,
            int submitid)
        {
            if (await cache.GetCachedSubmissionAsync(submitid) is not CacheResult.PostResult pr)
                return req.CreateResponse(HttpStatusCode.NotFound);

            var submission = pr.Post;

            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    return await req.WriteCrowmaskResponseAsync(format, ActivityPubSerializer.SerializeWithContext(translator.AsObject(submission)));
                }
                else if (format.Family.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(submission));
                }
                else if (format.Family.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(submission));
                }
                else if (format.Family.IsUpstreamRedirect)
                {
                    return req.Redirect(submission.url);
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
