using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Markdown;
using System.Net;
using Crowmask.DomainModeling;

namespace Crowmask.Functions
{
    public class Submissions(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, Translator translator)
    {
        [FunctionName("Submissions")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/submissions/{submitid}")] HttpRequest req,
            int submitid,
            ILogger log)
        {
            var submission = await crowmaskCache.GetSubmission(submitid);

            if (submission == null)
                return new NotFoundResult();

            foreach (var format in ContentNegotiation.FindAppropriateFormats(req.GetTypedHeaders().Accept))
            {
                if (format.IsActivityJson)
                {
                    return new ContentResult
                    {
                        Content = AP.SerializeWithContext(translator.AsObject(submission)),
                        ContentType = format.ContentType
                    };
                }
                else if (format.IsMarkdown)
                {
                    return new ContentResult
                    {
                        Content = markdownTranslator.ToMarkdown(submission),
                        ContentType = format.ContentType
                    };
                }
                else if (format.IsHTML)
                {
                    return new ContentResult
                    {
                        Content = markdownTranslator.ToHtml(submission),
                        ContentType = format.ContentType
                    };
                }
            }

            return new StatusCodeResult((int)HttpStatusCode.NotAcceptable);
        }
    }
}
