using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(
        ApplicationInformation appInfo,
        IActorKeyProvider keyProvider,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator,
        ActivityPubTranslator translator,
        UserCache userCache)
    {
        /// <summary>
        /// Returns information about the sole ActivityPub actor exposed by
        /// Crowmask, which is set up to mirror the configured Weasyl profile.
        /// </summary>
        /// <param name="req"></param>
        /// <returns>An ActivityStreams Person object or a Markdown or HTML response, depending on the user agent's Accept header.</returns>
        [Function("Actor")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor")] HttpRequestData req)
        {
            var person = await userCache.GetUserAsync();

            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    var key = await keyProvider.GetPublicKeyAsync();
                    string json = ActivityPubSerializer.SerializeWithContext(translator.PersonToObject(person, key, appInfo));

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
                else if (format.Family.IsHTML)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(person));
                }
                else if (format.Family.IsMarkdown)
                {
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(person));
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
