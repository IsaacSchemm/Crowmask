using Crowmask.ActivityPub;
using Crowmask.Data;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Crowmask.Remote
{
    public class Requester(ICrowmaskHost host, IHttpClientFactory httpClientFactory, IKeyProvider keyProvider)
    {
        public record RemoteActor(string Id, string? Name, string Inbox, string? SharedInbox) : IRemoteActorDisplay
        {
            string IRemoteActorDisplay.DisplayName => Name ?? Id;
        }

        /// <summary>
        /// Fetches and returns an actor.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<RemoteActor> FetchActorAsync(string url)
        {
            string json = await GetJsonAsync(new Uri(url));

            JObject document = JObject.Parse(json);
            JArray expansion = JsonLdProcessor.Expand(document);

            string id = expansion[0]["@id"].Value<string>();
            string name = expansion[0]["https://www.w3.org/ns/activitystreams#name"][0]["@value"].Value<string>();

            string inbox = expansion[0]["http://www.w3.org/ns/ldp#inbox"][0]["@id"].Value<string>();
            string? sharedInbox = null;

            foreach (var endpoint in expansion[0]["https://www.w3.org/ns/activitystreams#endpoints"])
            {
                foreach (var si in endpoint["https://www.w3.org/ns/activitystreams#sharedInbox"])
                {
                    sharedInbox = si["@id"].Value<string>();
                }
            }

            return new RemoteActor(
                Id: id,
                Name: name,
                Inbox: inbox,
                SharedInbox: sharedInbox);
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

        private IEnumerable<string> GetHeadersToSign(HttpRequestMessage req)
        {
            yield return $"(request-target): {req.Method.Method.ToLowerInvariant()} {req.RequestUri!.AbsolutePath}";
            yield return $"host: {req.Headers.Host}";
            yield return $"date: {req.Headers.Date:r}";
            if (req.Headers.TryGetValues("Digest", out var values))
            {
                yield return $"digest: {values.Single()}";
            }
        }

        private async Task AddSignatureAsync(HttpRequestMessage req)
        {
            string ds = string.Join("\n", GetHeadersToSign(req));
            byte[] data = Encoding.UTF8.GetBytes(ds);
            byte[] signature = await keyProvider.SignRsaSha256Async(data);
            string headerNames = "(request-target) host date";
            if (req.Headers.Contains("Digest"))
            {
                headerNames += " digest";
            }
            req.Headers.Add("Signature", $"keyId=\"https://{host.Hostname}/api/actor#main-key\",algorithm=\"rsa-sha256\",headers=\"{headerNames}\",signature=\"{Convert.ToBase64String(signature)}\"");
        }

        private async Task PostAsync(Uri url, string json)
        {
            byte[]? body = Encoding.UTF8.GetBytes(json);
            string? digest = Convert.ToBase64String(SHA256.Create().ComputeHash(body));

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Host = url.Host;
            req.Headers.Date = DateTime.UtcNow;
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", "1.0"));

            req.Headers.Add("Digest", $"SHA-256={digest}");

            await AddSignatureAsync(req);

            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/activity+json"));

            req.Content = new ByteArrayContent(body);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/activity+json");

            using var httpClient = httpClientFactory.CreateClient();

            using var res = await httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();
        }

        public async Task<string> GetJsonAsync(Uri url)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Host = url.Host;
            req.Headers.Date = DateTime.UtcNow;
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", "1.0"));

            await AddSignatureAsync(req);

            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/activity+json"));

            using var httpClient = httpClientFactory.CreateClient();

            using var res = await httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            return await res.Content.ReadAsStringAsync();
        }
    }
}
