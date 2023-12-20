using Crowmask.ActivityPub;
using Crowmask.Data;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Crowmask.Remote
{
    public class Requester(IKeyProvider keyProvider)
    {
        private static readonly HttpClient _httpClient = new();

        public record Actor(string Inbox, string? SharedInbox);

        /// <summary>
        /// Fetches and returns an actor at a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<Actor> FetchActorAsync(string url)
        {
            var res = await GetAsync(new Uri(url));

            string json = await res.Content.ReadAsStringAsync();

            JObject document = JObject.Parse(json);
            JArray expansion = JsonLdProcessor.Expand(document);

            string inbox = expansion[0]["http://www.w3.org/ns/ldp#inbox"][0]["@id"].Value<string>();
            string? sharedInbox = null;

            foreach (var endpoint in expansion[0]["https://www.w3.org/ns/activitystreams#endpoints"])
            {
                foreach (var si in endpoint["https://www.w3.org/ns/activitystreams#sharedInbox"])
                {
                    sharedInbox = si["@id"].Value<string>();
                }
            }

            return new Actor(inbox, sharedInbox);
        }

        public async Task SendAsync(string recipient, IDictionary<string, object> message)
        {
            var actor = await FetchActorAsync(recipient);
            var url = new Uri(actor.Inbox);
            await PostAsync(url, AP.SerializeWithContext(message));
        }

        public async Task SendAsync(OutboundActivity activity)
        {
            var url = new Uri(activity.Inbox);
            await PostAsync(url, activity.JsonBody);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, Uri url, string? jsonBody = null)
        {
            string fragment = url.AbsolutePath;
            byte[]? body = null;
            string? digest = null;

            if (jsonBody != null)
            {
                body = Encoding.UTF8.GetBytes(jsonBody);
                digest = Convert.ToBase64String(SHA256.Create().ComputeHash(body));
            }

            var req = new HttpRequestMessage(httpMethod, url);
            req.Headers.Host = url.Host;
            req.Headers.Date = DateTime.UtcNow;
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", "1.0"));

            if (digest != null)
            {
                req.Headers.Add("Digest", $"SHA-256={digest}");
            }

            List<string> headersForSigning = [
                $"(request-target): {httpMethod.Method.ToLowerInvariant()} {fragment}",
                $"host: {req.Headers.Host}",
                $"date: {req.Headers.Date:r}"
            ];

            if (digest != null)
            {
                headersForSigning.Add($"digest: SHA-256={digest}");
            }

            string ds = string.Join("\n", headersForSigning);
            byte[] data = Encoding.UTF8.GetBytes(ds);
            byte[] signature = await keyProvider.SignRsaSha256Async(data);

            req.Headers.Add("Signature", $"keyId=\"{AP.ACTOR}#main-key\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest\",signature=\"{Convert.ToBase64String(signature)}\"");

            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/activity+json"));

            if (body != null)
            {
                req.Content = new ByteArrayContent(body);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/activity+json");
            }

            var res = await _httpClient.SendAsync(req);

            res.EnsureSuccessStatusCode();

            return res;
        }

        private async Task<HttpResponseMessage> PostAsync(Uri url, string json)
        {
            return await SendAsync(HttpMethod.Post, url, json);
        }

        private async Task<HttpResponseMessage> GetAsync(Uri url)
        {
            return await SendAsync(HttpMethod.Get, url);
        }
    }
}
