using Crowmask.HighLevel;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class SubmissionAlt(SubmissionCache cache)
    {
        /// <summary>
        /// Updates alt text for a submission.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="submitid">The submission ID</param>
        /// <returns></returns>
        [Function("SubmissionAlt")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/submissions/{submitid}/alt")] HttpRequestData req,
            int submitid)
        {
            var submission = await cache.RefreshSubmissionAsync(submitid);
            if (submission is not CacheResult.PostResult pr)
                return req.CreateResponse(HttpStatusCode.NotFound);

            string altText = pr.Post.images
                .Select(x => x.alt)
                .FirstOrDefault();

            if (altText == null)
                return req.CreateResponse(HttpStatusCode.NotFound);

            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", "text/plain");
            await resp.WriteStringAsync(altText);
            return resp;
        }
    }
}
