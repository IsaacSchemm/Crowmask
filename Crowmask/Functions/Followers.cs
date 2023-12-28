using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Markdown;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Followers(CrowmaskDbContext context, Translator translator, MarkdownTranslator markdownTranslator)
    {
        [Function("Followers")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/followers")] HttpRequestData req)
        {
            var count = await context.Followers.CountAsync();

            var followerCollection = Domain.AsFollowerCollection(count);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
                    var outbox = translator.AsFollowers(followerCollection);

                    string json = AP.SerializeWithContext(outbox);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(followerCollection));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(followerCollection));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
