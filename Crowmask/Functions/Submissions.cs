using Crowmask.DomainModeling;
using Crowmask.Formats.ActivityPub;
using Crowmask.Formats.Markdown;
using Crowmask.Library.Cache;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Submissions(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, Translator translator)
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
            if (await crowmaskCache.GetSubmissionAsync(submitid) is not CacheResult.PostResult pr || pr.Post is not Post submission)
                return req.CreateResponse(HttpStatusCode.NotFound);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityStreams)
                {
                    // This endpoint also implements the collections for likes, shares, and (similar to PeerTube) comments.
                    // Most ActivityPub applications would store these interactions in their own tables and paginate them here, if they expose them at all.
                    // I wanted them visible on the HTML variant of the page, so I decided to include them.
                    var objectToSerialize =
                        req.Query["view"] == "comments" ? translator.AsCommentsCollection(submission)
                        : req.Query["view"] == "likes" ? translator.AsLikesCollection(submission)
                        : req.Query["view"] == "shares" ? translator.AsSharesCollection(submission)
                        : translator.AsObject(submission);
                    return await req.WriteCrowmaskResponseAsync(format, AP.SerializeWithContext(objectToSerialize));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(submission));
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(submission));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
