using Crowmask.Data;
using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class MentionNotification(
        CrowmaskDbContext context,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator,
        ActivityPubTranslator translator)
    {
        [Function("MentionNotification")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/mentions/{guid}/notification")] HttpRequestData req,
            Guid guid)
        {
            var mention = await context.Mentions
                .SingleOrDefaultAsync(i => i.Id == guid);
            if (mention == null)
                return req.CreateResponse(HttpStatusCode.NotFound);

            var domainMention = Domain.AsRemoteMention(mention);

            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    var objectToSerialize = translator.AsPrivateNote(domainMention);
                    return await req.WriteCrowmaskResponseAsync(format, ActivityPubSerializer.SerializeWithContext(objectToSerialize));
                }
                else if (format.Family.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(domainMention));
                }
                else if (format.Family.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(domainMention));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
