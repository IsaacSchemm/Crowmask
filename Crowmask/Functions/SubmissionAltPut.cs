using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class SubmissionAltPut(SubmissionCache cache, IWeasylApiKeyProvider weasylApiKeyProvider)
    {
        /// <summary>
        /// Updates alt text for a submission.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="submitid">The submission ID</param>
        /// <returns></returns>
        [Function("SubmissionAltPut")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "api/submissions/{submitid}/alt")] HttpRequestData req,
            int submitid)
        {
            if (!req.Headers.TryGetValues("X-Weasyl-API-Key", out IEnumerable<string> keys))
                return req.CreateResponse(HttpStatusCode.Forbidden);
            if (!keys.Contains(weasylApiKeyProvider.ApiKey))
                return req.CreateResponse(HttpStatusCode.Forbidden);

            using var sr = new StreamReader(req.Body);
            string newAltText = await sr.ReadToEndAsync();
            await cache.RefreshSubmissionAsync(submitid, altText: newAltText);
            return req.CreateResponse(HttpStatusCode.ResetContent);
        }
    }
}
