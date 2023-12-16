using Crowmask.Weasyl;
using System.Net.Http.Json;
using System.Text;

namespace CrosspostSharp3.Weasyl
{
    public class WeasylClient
    {
        private readonly HttpClient _httpClient = new();

        public WeasylClient(IWeasylApiKeyProvider apiKeyProvider)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Weasyl-API-Key", apiKeyProvider.ApiKey);
        }

        public struct GalleryRequestOptions
        {
            public DateTimeOffset? since;
            public int? count;
            public int? folderid;
            public int? backid;
            public int? nextid;
        }

        public async Task<WeasylGallery> GetUserGalleryAsync(string user, GalleryRequestOptions options = default)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            StringBuilder qs = new();
            if (options.since != null)
                qs.Append($"&since={options.since:o}");
            if (options.count != null)
                qs.Append($"&count={options.count}");
            if (options.folderid != null)
                qs.Append($"&folderid={options.folderid}");
            if (options.backid != null)
                qs.Append($"&backid={options.backid}");
            if (options.nextid != null)
                qs.Append($"&nextid={options.nextid}");

            using HttpResponseMessage resp = await _httpClient.GetAsync(
                $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(user)}/gallery?{qs}");
            return await resp.Content.ReadFromJsonAsync<WeasylGallery>()
                ?? throw new Exception("Null response from API");
        }

        public async Task<WeasylSubmissionDetail> GetSubmissionAsync(int submitid)
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                $"https://www.weasyl.com/api/submissions/{submitid}/view");
            return await resp.Content.ReadFromJsonAsync<WeasylSubmissionDetail>()
                ?? throw new Exception("Null response from API");
        }

        public async Task<WeasylUserProfile> GetUserAsync(string user)
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(user)}/view");
            return await resp.Content.ReadFromJsonAsync<WeasylUserProfile>()
                ?? throw new Exception("Null response from API");
        }

        public async Task<WeasylUserBase> WhoamiAsync()
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                $"https://www.weasyl.com/api/whoami");
            return await resp.Content.ReadFromJsonAsync<WeasylUserBase>()
                ?? throw new Exception("Null response from API");
        }
    }
}
