using Crowmask.HighLevel;
using Crowmask.Interfaces;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.FSharp.Control;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(
        IApplicationInformation appInfo,
        IActorKeyProvider keyProvider,
        MarkdownTranslator markdownTranslator,
        ContentNegotiator negotiator,
        SubmissionCache submissionCache,
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
                    var recent = await submissionCache.GetCachedSubmissionsAsync().Take(3).ToListAsync();
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToHtml(person, recent));
                }
                else if (format.Family.IsMarkdown)
                {
                    var recent = await submissionCache.GetCachedSubmissionsAsync().Take(3).ToListAsync();
                    return await req.WriteCrowmaskResponseAsync(format, markdownTranslator.ToMarkdown(person, recent));
                }
                else if (format.Family.IsRedirectActor)
                {
                    return req.Redirect(person.url);
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
