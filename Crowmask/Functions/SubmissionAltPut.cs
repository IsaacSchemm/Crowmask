using Crowmask.HighLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class SubmissionAltPut(SubmissionCache cache)
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
            if (DateTime.UtcNow > new DateTime(2024, 4, 28, 22, 0, 0, DateTimeKind.Utc))
                return req.CreateResponse(HttpStatusCode.Forbidden);

            using var sr = new StreamReader(req.Body);
            string newAltText = await sr.ReadToEndAsync();
            await cache.RefreshSubmissionAsync(submitid, altText: newAltText);
            return req.CreateResponse(HttpStatusCode.ResetContent);
        }
    }
}
