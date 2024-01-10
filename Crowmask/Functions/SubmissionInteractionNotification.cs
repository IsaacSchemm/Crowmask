using Crowmask.DomainModeling;
using Crowmask.Formats.ActivityPub;
using Crowmask.Formats.Markdown;
using Crowmask.Formats.Summaries;
using Crowmask.Library.Cache;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class SubmissionInteractionNotification(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, Translator translator)
    {
        [Function("SubmissionInteractionNotification")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/submissions/{submitid}/interactions/{guid}/notification")] HttpRequestData req,
            int submitid,
            Guid guid)
        {
            if (await crowmaskCache.GetSubmissionAsync(submitid) is not CacheResult.PostResult pr || pr.Post is not Post submission)
                return req.CreateResponse(HttpStatusCode.NotFound);

            if (!submission.Interactions.Any(i => i.Id == guid))
                return req.CreateResponse(HttpStatusCode.NotFound);

            var interaction = submission.Interactions.SingleOrDefault(i => i.Id == guid);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityStreams)
                {
                    var objectToSerialize = translator.AsPrivateNote(submission, interaction);
                    return await req.WriteCrowmaskResponseAsync(format, AP.SerializeWithContext(objectToSerialize));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(submission, interaction));
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(submission, interaction));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}