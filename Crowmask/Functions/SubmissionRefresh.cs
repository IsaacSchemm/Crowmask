using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class SubmissionRefresh(SubmissionCache cache, IWeasylApiKeyProvider weasylApiKeyProvider)
    {
        /// <summary>
        /// Updates a submission from Weasyl, if it is stale or missing in Crowmask's cache.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="submitid">The numeric ID of the submission on Weasyl</param>
        /// <returns></returns>
        [Function("SubmissionRefresh")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/submissions/{submitid}/refresh")] HttpRequestData req,
            int submitid)
        {
            if (!req.Headers.TryGetValues("X-Weasyl-API-Key", out IEnumerable<string> keys))
                return req.CreateResponse(HttpStatusCode.Forbidden);
            if (!keys.Contains(weasylApiKeyProvider.ApiKey))
                return req.CreateResponse(HttpStatusCode.Forbidden);

            await cache.RefreshSubmissionAsync(submitid);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
