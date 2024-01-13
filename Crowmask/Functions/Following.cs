using Crowmask.Formats.ActivityPub;
using Crowmask.Formats.ContentNegotiation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Following(Negotiator negotiator, Translator translator)
    {
        /// <summary>
        /// Returns an empty ActivityStreams Collection.
        /// </summary>
        /// <param name="req"></param>
        /// <returns>An ActivityStreams Collection object.</returns>
        [Function("Following")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/following")] HttpRequestData req)
        {
            foreach (var format in negotiator.GetAcceptableFormats(req.Headers))
            {
                if (format.Family.IsActivityPub)
                {
                    var coll = translator.FollowingCollection;

                    string json = AP.SerializeWithContext(coll);

                    return await req.WriteCrowmaskResponseAsync(format, json);
                }
            }

            return req.CreateResponse(HttpStatusCode.NotAcceptable);
        }
    }
}
