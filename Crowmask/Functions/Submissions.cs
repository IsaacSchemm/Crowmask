using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.DomainModeling;
using Crowmask.Markdown;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Submissions(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, Translator translator)
    {
        [Function("Submissions")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/submissions/{submitid}")] HttpRequestData req,
            int submitid)
        {
            if (await crowmaskCache.GetSubmissionAsync(submitid) is not CacheResult.PostResult pr || pr.Post is not Post submission)
                return req.CreateResponse(HttpStatusCode.NotFound);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
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
