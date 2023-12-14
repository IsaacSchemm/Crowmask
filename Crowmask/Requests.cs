using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    }
}
