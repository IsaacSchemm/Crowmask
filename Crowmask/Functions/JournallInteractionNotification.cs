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
    public class JournallInteractionNotification(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, Translator translator)
    {
        [Function("JournallInteractionNotification")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/journals/{journalid}/interactions/{guid}/notification")] HttpRequestData req,
            int journalid,
            Guid guid)
        {
            if (await crowmaskCache.GetJournalAsync(journalid) is not CacheResult.PostResult pr || pr.Post is not Post journal)
                return req.CreateResponse(HttpStatusCode.NotFound);

            if (!journal.Interactions.Any(i => i.Id == guid))
                return req.CreateResponse(HttpStatusCode.NotFound);

            var interaction = journal.Interactions.SingleOrDefault(i => i.Id == guid);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityStreams)
                {
                    var objectToSerialize = translator.AsPrivateNote(journal, interaction);
                    return await req.WriteCrowmaskResponseAsync(format, AP.SerializeWithContext(objectToSerialize));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(journal, interaction));
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(journal, interaction));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}