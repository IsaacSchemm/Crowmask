using Crowmask.HighLevel;
using Crowmask.HighLevel.Feed;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class OutboxPage(
        SubmissionCache cache,
        FeedBuilder feedBuilder,
        ActivityPubTranslator translator,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator,
        UserCache userCache)
    {
        /// <summary>
        /// Returns up to 20 of the user's posts mirrored from Weasyl and
        /// cached in Crowmask's database. Posts are rendered in reverse
        /// chronological order (newest first).
        /// </summary>
        /// <param name="req"></param>
        /// <returns>An ActivityStreams OrderedCollectionPage or a Markdown or HTML response, depending on the user agent's Accept header.</returns>
        [Function("OutboxPage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox/page")] HttpRequestData req)
        {
            int nextid = int.TryParse(req.Query["nextid"], out int n)
                ? n
                : int.MaxValue;

            var posts = await cache.GetCachedSubmissionsAsync(nextid: nextid)
                .Take(20)
                .ToListAsync();

            var galleryPage = Domain.AsGalleryPage(posts, nextid);

            var person = await userCache.GetUserAsync();

            var acceptableFormats =
                req.Query["format"] == "rss" ? [negotiator.RSS]
                : req.Query["format"] == "atom" ? [negotiator.Atom]
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
