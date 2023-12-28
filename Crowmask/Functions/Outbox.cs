using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Markdown;
using Crowmask.Weasyl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Outbox(CrowmaskCache crowmaskCache, Translator translator, MarkdownTranslator markdownTranslator, WeasylClient weasylClient)
    {
        [Function("Outbox")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox")] HttpRequestData req)
        {
            var whoami = await weasylClient.WhoamiAsync();

            var recent = await weasylClient.GetUserGallerySubmissionsAsync(whoami.login)
                .Take(20)
                .SelectAwait(async x => await crowmaskCache.GetSubmission(x.submitid))
                .ToListAsync();

            foreach (var format in ContentNegotiation.ForHeaders(req.Headers))
            {
                if (format.IsActivityJson)
                {
                    var outbox = translator.AsOutbox(recent);

                    string json = AP.SerializeWithContext(outbox);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(recent));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(recent));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
