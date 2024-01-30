using Crowmask.Data;
using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class InteractionNotification(
        CrowmaskDbContext context,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator,
        ActivityPubTranslator translator)
    {
        [Function("InteractionNotification")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/interactions/{guid}/notification")] HttpRequestData req,
            Guid guid)
        {
            var interaction = await context.Interactions
                .SingleOrDefaultAsync(i => i.Id == guid);
            if (interaction == null)
                return req.CreateResponse(HttpStatusCode.NotFound);

            var domainInteraction = Domain.AsRemoteInteraction(interaction);

            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    var objectToSerialize = translator.AsPrivateNote(domainInteraction);
                    return await req.WriteCrowmaskResponseAsync(format, ActivityPubSerializer.SerializeWithContext(objectToSerialize));
                }
                else if (format.Family.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(domainInteraction));
                }
                else if (format.Family.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(domainInteraction));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
