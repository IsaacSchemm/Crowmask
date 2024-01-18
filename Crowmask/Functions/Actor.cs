using Crowmask.Formats;
using Crowmask.Interfaces;
using Crowmask.Library;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(
        ICrowmaskKeyProvider keyProvider,
        IHandleName handleName,
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

            var key = await keyProvider.GetPublicKeyAsync();

            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    string json = ActivityPubSerializer.SerializeWithContext(translator.PersonToObject(person, key, handleName));

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
                else if (format.Family.IsUpstreamRedirect)
                {
                    return req.Redirect(person.url);
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
