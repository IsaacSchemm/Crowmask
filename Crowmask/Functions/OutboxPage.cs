using Crowmask.DomainModeling;
using Crowmask.Formats;
using Crowmask.Library.Cache;
using Crowmask.Library.Feed;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class OutboxPage(
        CrowmaskCache crowmaskCache,
        FeedBuilder feedBuilder,
        ActivityPubTranslator translator,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator)
    {
        /// <summary>
        /// Returns up to 20 of the user's posts (submissions and journals)
        /// mirrored from Weasyl and cached in Crowmask's database. Posts are
        /// rendered in reverse chronological order (newest first) by
        /// combining submission and journal streams from Crowmask's cache.
        /// </summary>
        /// <param name="req"></param>
        /// <returns>An ActivityStreams OrderedCollectionPage or a Markdown or HTML response, depending on the user agent's Accept header.</returns>
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
                req.Query["format"] == "rss" ? [ContentNegotiation.RSS]
                : req.Query["format"] == "atom" ? [ContentNegotiation.Atom]
                : negotiator.GetAcceptableFormats(req.Headers);

            foreach (var format in acceptableFormats)
            {
                if (format.Family.IsActivityPub)
                {
                    var outboxPage = translator.AsOutboxPage(req.Url.OriginalString, galleryPage);

                    string json = ActivityPubSerializer.SerializeWithContext(outboxPage);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.Family.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(galleryPage));
                }
                else if (format.Family.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(galleryPage));
                }
                else if (format.Family.IsRSS)
                {
                    return await req.WriteCrowmaskResponseAsync(format, feedBuilder.ToRssFeed(person, galleryPage.posts));
                }
                else if (format.Family.IsAtom)
                {
                    return await req.WriteCrowmaskResponseAsync(format, feedBuilder.ToAtomFeed(person, galleryPage.posts));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
