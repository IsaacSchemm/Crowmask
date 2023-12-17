using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Crowmask.ActivityPub;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crowmask
{
    public static class Requests
    {
        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Fetches and returns an actor at a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<Actor> FetchActorAsync(string url)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/activity+json"));

            var res = await _httpClient.SendAsync(req);

            res.EnsureSuccessStatusCode();

            // TODO implement JSON-LD here

            var body = await res.Content.ReadFromJsonAsync<Actor>();
            return body;
        }

        private static CryptographyClient GetCryptographyClient()
        {
            var credential = new DefaultAzureCredential();
            var uri = new Uri("https://crowmask.vault.azure.net/");
            var keyClient = new KeyClient(uri, credential);
            return keyClient.GetCryptographyClient("crowmask-ap");
        }

        public static async Task<HttpResponseMessage> SendAsync(string sender, string recipient, AP.Object message)
        {
            var url = new Uri(recipient);

            var actor = await FetchActorAsync(recipient);
            var fragment = actor.inbox.Replace($"https://{url.Host}", "");
            var json = JsonSerializer.Serialize(message);
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

            var req = new HttpRequestMessage(HttpMethod.Post, actor.inbox);
            req.Headers.Host = url.Host;
            req.Headers.Date = d;
            req.Headers.Add("Digest", $"SHA-256={digest}");
            req.Headers.Add("Signature", $"keyId=\"{sender}#main-key\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest\",signature=\"{Convert.ToBase64String(signature)}\"");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            req.Content = new ByteArrayContent(body);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var res = await _httpClient.SendAsync(req);

            res.EnsureSuccessStatusCode();

            return res;
        }
    }
}
