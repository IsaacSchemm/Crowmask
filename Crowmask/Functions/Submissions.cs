using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Markdown;
using Microsoft.AspNetCore.Mvc;
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
            var submission = await crowmaskCache.GetSubmission(submitid);

            if (submission == null)
                return req.CreateResponse(HttpStatusCode.NotFound);

            foreach (var format in ContentNegotiation.ForHeaders(req.Headers))
            {
                if (format.IsActivityJson)
                {
                    return await req.WriteCrowmaskResponseAsync(format, AP.SerializeWithContext(translator.AsObject(submission)));
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
