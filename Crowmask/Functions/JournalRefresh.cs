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
    public class JournalRefresh(SubmissionCache cache, IWeasylApiKeyProvider weasylApiKeyProvider)
    {
        /// <summary>
        /// Updates a journal entry from Weasyl, if it is stale or missing in Crowmask's cache.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="journalid">The numeric ID of the journal on Weasyl</param>
        /// <returns></returns>
        [Function("JournalRefresh")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/journals/{journalid}/refresh")] HttpRequestData req,
            int journalid)
        {
            if (!req.Headers.TryGetValues("X-Weasyl-API-Key", out IEnumerable<string> keys))
                return req.CreateResponse(HttpStatusCode.Forbidden);
            if (!keys.Contains(weasylApiKeyProvider.ApiKey))
                return req.CreateResponse(HttpStatusCode.Forbidden);

            await cache.RefreshJournalAsync(journalid);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
