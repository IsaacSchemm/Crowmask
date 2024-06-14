using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class WebFinger(ApplicationInformation appInfo, IHttpClientFactory httpClientFactory)
    {
        [Function("WebFinger")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/webfinger")] HttpRequestData req)
        {
            if (req.Query["resource"] is not string resource)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            foreach (string hostname in appInfo.WebFingerDomains)
            {
                var uri = new Uri("https://" + hostname);
                var webFingerUri = new Uri(uri, $"/.well-known/webfinger?resource={Uri.EscapeDataString(resource)}");

                using var httpClient = httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(webFingerUri);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                var resp = req.CreateResponse(HttpStatusCode.OK);
                resp.Headers.Add("Content-Type", response.Content.Headers.ContentType.MediaType);
                await resp.WriteStringAsync(json, Encoding.UTF8);

                return resp;
            }

            return req.CreateResponse(HttpStatusCode.NotFound);
        }
    }
}
