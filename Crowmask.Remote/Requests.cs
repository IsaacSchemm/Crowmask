using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Crowmask.ActivityPub;
using Crowmask.Data;
using JsonLD.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Crowmask.Remote
{
    public static class Requests
    {
        private static readonly HttpClient _httpClient = new();

        private record Actor(string Inbox);

        /// <summary>
        /// Fetches and returns an actor at a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<Actor> FetchActorAsync(string url)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/activity+json"));

            var res = await _httpClient.SendAsync(req);

            res.EnsureSuccessStatusCode();

            string json = await res.Content.ReadAsStringAsync();

            JObject document = JObject.Parse(json);
            string inbox = JsonLdProcessor.Expand(document)[0]["http://www.w3.org/ns/ldp#inbox"][0]["@id"].Value<string>();

            return new Actor(inbox);
        }

        private static CryptographyClient GetCryptographyClient()
        {
            var credential = new DefaultAzureCredential();
            var uri = new Uri("https://crowmask.vault.azure.net/");
            var keyClient = new KeyClient(uri, credential);
            return keyClient.GetCryptographyClient("crowmask-ap");
        }

        public static async Task SendAsync(string recipient, IDictionary<string, object> message)
        {
            var actor = await FetchActorAsync(recipient);
            var url = new Uri(actor.Inbox);
            await SendAsync(url, AP.SerializeWithContext(message));
        }

        public static async Task SendAsync(OutboundActivity activity)
        {
            var url = new Uri(activity.Inbox);
            await SendAsync(url, activity.JsonBody);
        }

        private static async Task SendAsync(Uri url, string json)
        {
            var fragment = url.AbsolutePath;
            var body = Encoding.UTF8.GetBytes(json);
            var digest = Convert.ToBase64String(SHA256.Create().ComputeHash(body));
            var d = DateTime.UtcNow;

            string ds = string.Join("\n", [
                $"(request-target): post {fragment}",
                $"host: {url.Host}",
                $"date: {d:r}",
                $"digest: SHA-256={digest}"
            ]);

            var data = Encoding.UTF8.GetBytes(ds);

            var signResult = GetCryptographyClient().SignData(SignatureAlgorithm.RS256, data);
            byte[] signature = signResult.Signature;

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Host = url.Host;
            req.Headers.Date = d;
            req.Headers.Add("Digest", $"SHA-256={digest}");
            req.Headers.Add("Signature", $"keyId=\"{AP.ACTOR}#main-key\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest\",signature=\"{Convert.ToBase64String(signature)}\"");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            req.Content = new ByteArrayContent(body);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var res = await _httpClient.SendAsync(req);

            res.EnsureSuccessStatusCode();
        }
    }
}
