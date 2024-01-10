using Crowmask.Data;
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
    public class Journals(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, Translator translator)
    {
        /// <summary>
        /// Returns a mirror of a journal posted to Weasyl.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="journalid">The numeric ID of the journal on Weasyl</param>
        /// <returns>An ActivityStreams Article or Collection or a Markdown or HTML response, depending on the query string and the user agent's Accept header.</returns>
        [Function("Journals")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/journals/{journalid}")] HttpRequestData req,
            int journalid)
        {
            if (await crowmaskCache.GetJournalAsync(journalid) is not CacheResult.PostResult pr || pr.Post is not Post journal)
                return req.CreateResponse(HttpStatusCode.NotFound);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityStreams)
                {
                    // This endpoint also implements the collections for likes, shares, and (similar to PeerTube) comments.
                    // Most ActivityPub applications would store these interactions in their own tables and paginate them here, if they expose them at all.
                    // I wanted them visible on the HTML variant of the page, so I decided to include them.
                    var objectToSerialize =
                        req.Query["view"] == "comments" ? translator.AsCommentsCollection(journal)
                        : req.Query["view"] == "likes" ? translator.AsLikesCollection(journal)
                        : req.Query["view"] == "shares" ? translator.AsSharesCollection(journal)
                        : req.Query["view"] == "create" ? translator.ObjectToCreate(journal)
                        : translator.AsObject(journal);
                    return await req.WriteCrowmaskResponseAsync(format, AP.SerializeWithContext(objectToSerialize));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(journal));
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(journal));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
