using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Markdown;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(CrowmaskCache crowmaskCache, IPublicKeyProvider keyProvider, MarkdownTranslator markdownTranslator, Translator translator)
    {
        [Function("Actor")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor")] HttpRequestData req)
        {
            var person = await crowmaskCache.UpdateUserAsync();

            var key = await keyProvider.GetPublicKeyAsync();

            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
                {
                    string json = AP.SerializeWithContext(translator.PersonToObject(person, key));

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(person));
                }
                else if (format.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(person));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
