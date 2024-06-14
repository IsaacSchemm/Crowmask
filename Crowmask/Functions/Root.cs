using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Crowmask.Functions
{
    public class Root
    {
        /// <summary>
        /// Redirects to the actor URL.
        /// </summary>
        /// <param name="req">The HTTP request</param>
        [Function("Root")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/")] HttpRequestData req)
        {
            var resp = req.CreateResponse(HttpStatusCode.TemporaryRedirect);
            resp.Headers.Add("Location", "https://github.com/IsaacSchemm/Crowmask/");
            return resp;
        }
    }
}
