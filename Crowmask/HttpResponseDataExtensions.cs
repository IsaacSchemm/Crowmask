using Crowmask.Markdown;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask
{
    public static class HttpRequestDataExtensions
    {
        public static async Task<HttpResponseData> WriteCrowmaskResponseAsync(
            this HttpRequestData req,
            CrowmaskFormat format,
            string content)
        {
            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", format.ContentType);
            await resp.WriteStringAsync(content);
            return resp;
        }

        public static IEnumerable<CrowmaskFormat> GetAcceptableCrowmaskFormats(this HttpRequestData req)
        {
            var headers = req.Headers.GetValues("Accept")
                .Select(str => MediaTypeHeaderValue.Parse(str))
                .OrderByDescending(h => h, MediaTypeHeaderValueComparer.QualityComparer);
            foreach (var accept in headers)
                foreach (var format in CrowmaskFormat.All)
                    foreach (var type in format.MediaTypes)
                        if (type.IsSubsetOf(accept))
                            yield return format;
        }
    }
}
