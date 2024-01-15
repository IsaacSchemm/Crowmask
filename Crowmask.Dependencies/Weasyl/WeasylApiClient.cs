using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Crowmask.Dependencies.Weasyl
{
    internal partial class WeasylApiClient(IHttpClientFactory httpClientFactory, IWeasylApiKeyProvider apiKeyProvider)
    {
        [GeneratedRegex(@"/journal/([0-9]+)")]
        private static partial Regex JournalUriPattern();

        private async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken, string? accept = null)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", "1.1"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(accept ?? "application/json"));
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

        public async IAsyncEnumerable<int> GetJournalIdsAsync(string login_name, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var resp = await GetAsync(
                $"https://www.weasyl.com/journals/{Uri.EscapeDataString(login_name)}",
                cancellationToken,
                accept: "text/html");

            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(cancellationToken);

            var matches = JournalUriPattern().Matches(html);
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                yield return int.Parse(match.Groups[1].Value);
            }
        }

        public async Task<WeasylJournalDetail?> GetJournalAsync(int journalid, CancellationToken cancellationToken = default)
        {
            using var resp = await GetAsync(
                $"https://www.weasyl.com/api/journals/{journalid}/view",
                cancellationToken);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WeasylJournalDetail>(cancellationToken)
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
