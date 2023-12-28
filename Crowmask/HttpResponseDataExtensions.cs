using Crowmask.Markdown;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask
{
    public static class HttpRequestDataExtensions
    {
        public static async Task<HttpResponseData> WriteCrowmaskResponseAsync(
            this HttpRequestData req,
            ContentNegotiation.CrowmaskFormat format,
            string content)
        {
            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", format.ContentType);
            await resp.WriteStringAsync(content);
            return resp;
        }
    }
}
