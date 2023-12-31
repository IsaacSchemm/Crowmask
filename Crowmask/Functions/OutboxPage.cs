using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.DomainModeling;
using Crowmask.Markdown;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class OutboxPage(CrowmaskCache crowmaskCache, Translator translator, MarkdownTranslator markdownTranslator, WeasylUserClient weasylUserClient)
    {
        [Function("OutboxPage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox/page")] HttpRequestData req)
        {
            var gallery = await weasylUserClient.GetMyGalleryAsync(
                count: 20,
                nextid: int.TryParse(req.Query["nextid"], out int n) ? n : null,
                backid: int.TryParse(req.Query["backid"], out int b) ? b : null);

            var submissions = await gallery.submissions
                .ToAsyncEnumerable()
                .SelectAwait(async s => await crowmaskCache.UpdateSubmissionAsync(s.submitid))
                .SelectMany(obj => obj.AsList.ToAsyncEnumerable())
                .ToListAsync();

            var galleryPage = Domain.AsPostList(
                submissions,
                nextid: gallery.nextid,
                backid: gallery.backid);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
                    var outboxPage = translator.AsOutboxPage(req.Url.OriginalString, galleryPage);

                    string json = AP.SerializeWithContext(outboxPage);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(galleryPage));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(galleryPage));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
