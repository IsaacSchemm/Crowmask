using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.Markdown;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class FollowersPage(CrowmaskDbContext context, Translator translator, MarkdownTranslator markdownTranslator)
    {
        [Function("FollowersPage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/followers/page")] HttpRequestData req)
        {
            Guid? after = Guid.TryParse(req.Query["after"], out Guid a) ? a : null;

            var followers = await context.Followers
                .Where(f => after == null || f.Id > after)
                .OrderBy(f => f.Id)
                .ToListAsync();

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
                    var outbox = translator.AsFollowersPage(req.Url.OriginalString, followers);

                    string json = AP.SerializeWithContext(outbox);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(followers));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(followers));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
