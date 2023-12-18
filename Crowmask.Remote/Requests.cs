using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Crowmask.ActivityPub;
using Crowmask.Data;
using JsonLD.Core;
using Microsoft.FSharp.Collections;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Crowmask.Remote
{
    public static class Requests
    {
        private static readonly HttpClient _httpClient = new();

        public record PublicKey(string Id, string Pem);

        public record Actor(string Inbox, FSharpSet<PublicKey> PublicKeys);

        /// <summary>
        /// Fetches and returns an actor at a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<Actor> FetchActorAsync(string url)
        {
            var res = await GetAsync(new Uri(url));

            string json = await res.Content.ReadAsStringAsync();

            JObject document = JObject.Parse(json);
            JArray expansion = JsonLdProcessor.Expand(document);

            string inbox = expansion[0]["http://www.w3.org/ns/ldp#inbox"][0]["@id"].Value<string>();

            IEnumerable<PublicKey> getPublicKeys()
            {
                foreach (var k in expansion[0]["https://w3id.org/security#publicKey"])
                {
                    yield return new PublicKey(
                        k["@id"].Value<string>(),
                        k["https://w3id.org/security#publicKeyPem"][0]["@value"].Value<string>());
                }
            }

            return new Actor(
                inbox,
                SetModule.OfSeq(getPublicKeys()));
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
            await PostAsync(url, AP.SerializeWithContext(message));
        }

        public static async Task SendAsync(OutboundActivity activity)
        {
            var url = new Uri(activity.Inbox);
            await PostAsync(url, activity.JsonBody);
        }

        private static async Task<HttpResponseMessage> PostAsync(Uri url, string json)
        {
            string fragment = url.AbsolutePath;
            byte[] body = Encoding.UTF8.GetBytes(json);
            string digest = Convert.ToBase64String(SHA256.Create().ComputeHash(body));

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Host = url.Host;
            req.Headers.Date = DateTime.UtcNow;
            req.Headers.Add("Digest", $"SHA-256={digest}");
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", "1.0"));

            string ds = string.Join("\n", [
                $"(request-target): post {fragment}",
                $"host: {req.Headers.Host}",
                $"date: {req.Headers.Date:r}",
                $"digest: SHA-256={digest}"
            ]);

            byte[] data = Encoding.UTF8.GetBytes(ds);

            var signResult = GetCryptographyClient().SignData(SignatureAlgorithm.RS256, data);
            byte[] signature = signResult.Signature;

            req.Headers.Add("Signature", $"keyId=\"{AP.ACTOR}#main-key\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest\",signature=\"{Convert.ToBase64String(signature)}\"");

            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/activity+json"));

            req.Content = new ByteArrayContent(body);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/activity+json");

            var res = await _httpClient.SendAsync(req);

            res.EnsureSuccessStatusCode();

            return res;
        }

        private static async Task<HttpResponseMessage> GetAsync(Uri url)
        {
            string fragment = url.AbsolutePath;

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Host = url.Host;
            req.Headers.Date = DateTime.UtcNow;
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", "1.0"));

            string ds = string.Join("\n", [
                $"(request-target): post {fragment}",
                $"host: {req.Headers.Host}",
                $"date: {req.Headers.Date:r}"
            ]);

            byte[] data = Encoding.UTF8.GetBytes(ds);

            var signResult = GetCryptographyClient().SignData(SignatureAlgorithm.RS256, data);
            byte[] signature = signResult.Signature;

            req.Headers.Add("Signature", $"keyId=\"{AP.ACTOR}#main-key\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest\",signature=\"{Convert.ToBase64String(signature)}\"");

            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/activity+json"));

            var res = await _httpClient.SendAsync(req);

            res.EnsureSuccessStatusCode();

            return res;
        }
    }
}
