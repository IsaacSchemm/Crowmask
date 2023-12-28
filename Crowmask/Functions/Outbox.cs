using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Markdown;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Outbox(Translator translator, MarkdownTranslator markdownTranslator, WeasylClient weasylClient)
    {
        [Function("Outbox")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/outbox")] HttpRequestData req)
        {
            var whoami = await weasylClient.WhoamiAsync();

            var user = await weasylClient.GetUserAsync(whoami.login);

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
                    var outbox = translator.AsOutbox(totalItems: user.statistics.submissions);

                    string json = AP.SerializeWithContext(outbox);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(outbox: []));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(outbox: []));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
