using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Journal(
        SubmissionCache cache,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator,
        ActivityPubTranslator translator)
    {
        /// <summary>
        /// Returns a mirror of a journal entry posted to Weasyl.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="journalid">The numeric ID of the journal on Weasyl</param>
        /// <returns>An ActivityStreams Note or Collection or a Markdown or HTML response, depending on the query string and the user agent's Accept header.</returns>
        [Function("Journal")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/journals/{journalid}")] HttpRequestData req,
            int journalid)
        {
            if (await cache.RefreshJournalAsync(journalid) is not CacheResult.PostResult pr)
                return req.CreateResponse(HttpStatusCode.NotFound);

            var post = pr.Post;

            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    return await req.WriteCrowmaskResponseAsync(format, ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)));
                }
                else if (format.Family.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(post));
                }
                else if (format.Family.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(post));
                }
                else if (format.Family.IsUpstreamRedirect)
                {
                    return req.Redirect(post.url);
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
