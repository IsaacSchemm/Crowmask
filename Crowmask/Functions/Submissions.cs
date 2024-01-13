using Crowmask.DomainModeling;
using Crowmask.Formats;
using Crowmask.Library.Cache;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Submissions(
        CrowmaskCache crowmaskCache,
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
            if (await crowmaskCache.GetSubmissionAsync(submitid) is not CacheResult.PostResult pr)
                return req.CreateResponse(HttpStatusCode.NotFound);

            var submission = pr.Post;

            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    // This endpoint also implements the collections for likes, shares, and (similar to PeerTube) comments.
                    // Most ActivityPub applications would store these interactions in their own tables and paginate them here, if they expose them at all.
                    // I wanted them visible on the HTML variant of the page, so I decided to include them.
                    var objectToSerialize =
                        req.Query["view"] == "comments" ? translator.AsCommentsCollection(submission)
                        : req.Query["view"] == "likes" ? translator.AsLikesCollection(submission)
                        : req.Query["view"] == "shares" ? translator.AsSharesCollection(submission)
                        : req.Query["view"] == "create" ? translator.ObjectToCreate(submission)
                        : translator.AsObject(submission);
                    return await req.WriteCrowmaskResponseAsync(format, ActivityPubSerializer.SerializeWithContext(objectToSerialize));
                }
                else if (format.Family.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(submission));
                }
                else if (format.Family.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(submission));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
