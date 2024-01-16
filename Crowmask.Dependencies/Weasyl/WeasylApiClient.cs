using Crowmask.Interfaces;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Crowmask.Dependencies.Weasyl
{
    internal class WeasylApiClient(ICrowmaskVersion version, IHttpClientFactory httpClientFactory, IWeasylApiKeyProvider apiKeyProvider)
    {
        private async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", version.VersionNumber));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("X-Weasyl-API-Key", apiKeyProvider.ApiKey);

            return await httpClient.GetAsync(uri, cancellationToken);
        }

        public async Task<WeasylGallery> GetUserGalleryAsync(string username, int? count = null, int? nextid = null, int? backid = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<string> query()
            {
                if (count is int c) yield return $"count={c}";
                if (nextid is int n) yield return $"nextid={n}";
                if (backid is int b) yield return $"backid={b}";
            }

            using var resp = await GetAsync(
                $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(username)}/gallery?{string.Join("&", query())}",
                cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WeasylGallery>(cancellationToken)
                ?? throw new NotImplementedException();
        }

        public async Task<WeasylSubmissionDetail?> GetSubmissionAsync(int submitid, CancellationToken cancellationToken = default)
        {
            using var resp = await GetAsync(
                $"https://www.weasyl.com/api/submissions/{submitid}/view",
                cancellationToken);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WeasylSubmissionDetail>(cancellationToken)
                ?? throw new NotImplementedException();
        }

        public async Task<WeasylUserProfile> GetUserAsync(string user, CancellationToken cancellationToken = default)
        {
            using var resp = await GetAsync(
                $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(user)}/view",
                cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WeasylUserProfile>(cancellationToken)
                ?? throw new NotImplementedException();
        }

        public async Task<WeasylWhoami> WhoamiAsync(CancellationToken cancellationToken = default)
        {
            using var resp = await GetAsync(
                $"https://www.weasyl.com/api/whoami",
                cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WeasylWhoami>(cancellationToken)
                ?? throw new NotImplementedException();
        }
    }
}
