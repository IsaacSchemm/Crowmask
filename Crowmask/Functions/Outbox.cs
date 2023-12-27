using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.DomainModeling;
using Crowmask.Markdown;
using Crowmask.Weasyl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Outbox(CrowmaskCache crowmaskCache, Translator translator, MarkdownTranslator markdownTranslator, WeasylClient weasylClient)
    {
        [FunctionName("Outbox")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox")] HttpRequest req,
            ILogger log)
        {
            var whoami = await weasylClient.WhoamiAsync();

            var recent = await weasylClient.GetUserGallerySubmissionsAsync(whoami.login)
                .Take(20)
                .SelectAwait(async x => await crowmaskCache.GetSubmission(x.submitid))
                .ToListAsync();

            foreach (var format in ContentNegotiation.FindAppropriateFormats(req.GetTypedHeaders().Accept))
            {
                if (format.IsActivityJson)
                {
                    var outbox = translator.AsOutbox(recent);

                    string json = AP.SerializeWithContext(outbox);

                    return new ContentResult
                    {
                        Content = json,
                        ContentType = format.ContentType
                    };
                }
                else if (format.IsHTML)
                {
                    return new ContentResult
                    {
                        Content = markdownTranslator.ToHtml(recent),
                        ContentType = format.ContentType
                    };
                }
                else if (format.IsMarkdown)
                {
                    return new ContentResult
                    {
                        Content = markdownTranslator.ToMarkdown(recent),
                        ContentType = format.ContentType
                    };
                }
            }

            return new StatusCodeResult((int)HttpStatusCode.NotAcceptable);
        }
    }
}
