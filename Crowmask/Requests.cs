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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Enforce lowercase for  ActivityPub interoperability")]
    public static class Requests
    {
        public record Endpoint(string sharedInbox = null);

        public record PublicKey(string publicKeyPem = null);

        public record Actor(
            string inbox,
            string outbox = null,
            string followers = null,
            string following = null,
            Endpoint endpoints = null,
            PublicKey publicKey = null);

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

            var body = await res.Content.ReadFromJsonAsync<Actor>();
            return body;
        }

        public static async Task<HttpResponseMessage> SendAsync(string sender, string recipient, object message)
        {
            var url = new Uri(recipient);

            var actor = await FetchActorAsync(recipient);
            var fragment = actor.inbox.Replace($"https://{url.Host}", "");
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var digest = Convert.ToBase64String(SHA256.Create().ComputeHash(body));
            var d = DateTime.UtcNow;

            var data = Encoding.UTF8.GetBytes(string.Join("\n", [
                $"(request-target): post {fragment}",
                $"host: {url.Host}",
                $"date: {d:r}",
                $"digest: SHA-256={digest}"
            ]));

            using RSA rsa = RSA.Create();
            //rsa.ImportParameters();
            throw new NotImplementedException();

            var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
            rsaFormatter.SetHashAlgorithm(nameof(SHA256));

            byte[] signature = rsaFormatter.CreateSignature(SHA256.Create().ComputeHash(data));

            var req = new HttpRequestMessage(HttpMethod.Post, actor.inbox);
            req.Headers.Host = url.Host;
            req.Headers.Date = d;
            req.Headers.Add("Digest", $"SHA-256={digest}");
            req.Headers.Add("Signature", $"keyId=\"{sender}#main-key\",headers=\"(request-target) host date digest\",signature=\"{signature}\"");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            req.Content = new ByteArrayContent(data);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var res = await _httpClient.SendAsync(req);

            res.EnsureSuccessStatusCode();

            return res;
        }
    }
}
