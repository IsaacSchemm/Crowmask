using Crowmask.Cache;
using Crowmask.Markdown;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class ActorJournals(CrowmaskCache crowmaskCache, MarkdownTranslator markdownTranslator, WeasylUserClient weasylUserClient)
    {
        [Function("ActorJournals")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/journals")] HttpRequestData req)
        {
            var journals = await weasylUserClient.GetMyJournalIdsAsync()
                .SelectAwait(async journalid => await crowmaskCache.GetJournalAsync(journalid))
                .Where(obj => obj != null)
                .ToListAsync();

            var page = DomainModeling.Domain.AsPostList(
                journals,
                backid: null,
                nextid: null);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(page));
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(page));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
