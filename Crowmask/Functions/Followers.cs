using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Markdown;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Followers(CrowmaskDbContext context, Translator translator, MarkdownTranslator markdownTranslator)
    {
        /// <summary>
        /// Returns a list of ActivityPub actors that follow this Crowmask instance.
        /// </summary>
        /// <param name="req"></param>
        /// <returns>An ActivityStreams Collection object or a Markdown or HTML response, depending on the user agent's Accept header.</returns>
        [Function("Followers")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/followers")] HttpRequestData req)
        {
            var followers = await context.Followers
                .OrderBy(f => f.ActorId)
                .ToListAsync();

            var followerCollection = Domain.AsFollowerCollection(followers);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
                    var coll = translator.AsFollowersCollection(followerCollection);

                    string json = AP.SerializeWithContext(coll);

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
