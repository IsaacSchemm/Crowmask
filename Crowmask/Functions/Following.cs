using Crowmask.ActivityPub;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Following(Translator translator)
    {
        [Function("Following")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/following")] HttpRequestData req)
        {
            foreach (var format in req.GetAcceptableCrowmaskFormats())
            {
                if (format.IsActivityJson)
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
