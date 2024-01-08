using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.DomainModeling;
using Crowmask.Feed;
using Crowmask.Markdown;
using Crowmask.Merging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class OutboxPage(CrowmaskCache crowmaskCache, FeedBuilder feedBuilder, Translator translator, MarkdownTranslator markdownTranslator)
    {
        [Function("OutboxPage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox/page")] HttpRequestData req)
        {
            int offset = int.TryParse(req.Query["offset"], out int n) ? n : 0;

            var posts = await crowmaskCache.GetAllCachedPostsAsync()
                .Skip(offset)
                .Take(20)
                .ToListAsync();

            var galleryPage = Domain.AsPage(posts, offset);

            var person = await crowmaskCache.GetUserAsync();

            var acceptableFormats =
                req.Query["format"] == "rss" ? [CrowmaskFormat.RSS]
                : req.Query["format"] == "atom" ? [CrowmaskFormat.Atom]
                : req.GetAcceptableCrowmaskFormats();

            foreach (var format in acceptableFormats)
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
                else if (format.IsRSS)
                {
                    return await req.WriteCrowmaskResponseAsync(format, feedBuilder.ToRssFeed(person, galleryPage.posts));
                }
                else if (format.IsAtom)
                {
                    return await req.WriteCrowmaskResponseAsync(format, feedBuilder.ToAtomFeed(person, galleryPage.posts));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
