using Microsoft.FSharp.Collections;
using System.Net.Http.Json;

namespace Crowmask.Weasyl
{
    public record WeasylMediaFile(
        int? mediaid,
        string url);

    public record WeasylUserBase(
        string login,
        int userid);

    public record WeasylUserMedia(
        FSharpList<WeasylMediaFile> avatar);

    public record WeasylStatistics(
        int submissions);

    public record WeasylUserInfo(
        int? age,
        string gender,
        string location,
        FSharpMap<string, FSharpList<string>> user_links);

    public record WeasylUserProfile(
        string username,
        string full_name,
        string profile_text,
        WeasylUserMedia media,
        string login_name,
        WeasylStatistics statistics,
        WeasylUserInfo user_info,
        string link);

    public record WeasylSubmissionMedia(
        FSharpList<WeasylMediaFile> submission,
        FSharpList<WeasylMediaFile> thumbnail);

    public record WeasylGallerySubmission(
        DateTime posted_at,
        int submitid);

    public record WeasylSubmissionDetail(
        string link,
        WeasylSubmissionMedia media,
        string owner,
        DateTime posted_at,
        string rating,
        string title,
        bool friends_only,
        FSharpSet<string> tags,
        int submitid,
        string subtype,
        string description);

    public record WeasylGallery(
        FSharpList<WeasylGallerySubmission> submissions,
        int? backid,
        int? nextid);

    public class WeasylClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWeasylApiKeyProvider _apiKeyProvider;

        public WeasylClient(IHttpClientFactory httpClientFactory, IWeasylApiKeyProvider apiKeyProvider)
        {
            _httpClientFactory = httpClientFactory;
            _apiKeyProvider = apiKeyProvider;
        }

        private async Task<T> GetJsonAsync<T>(string uri)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("X-Weasyl-API-Key", _apiKeyProvider.ApiKey);

            using HttpResponseMessage resp = await httpClient.GetAsync(uri);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<T>()
                ?? throw new Exception("Null response from API");
        }

        public async Task<WeasylGallery> GetUserGalleryAsync(string username, int? count = null, int? nextid = null, int? backid = null)
        {
            IEnumerable<string> query()
            {
                if (count is int c) yield return $"count={c}";
                if (nextid is int n) yield return $"nextid={n}";
                if (backid is int b) yield return $"backid={b}";
            }

            return await GetJsonAsync<WeasylGallery>(
                $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(username)}/gallery?{string.Join("&", query())}");
        }

        public async IAsyncEnumerable<WeasylGallerySubmission> GetUserGallerySubmissionsAsync(string username)
        {
            var gallery = await GetUserGalleryAsync(username);

            while (true)
            {
                foreach (var submission in gallery.submissions)
                {
                    yield return submission;
                }

                if (gallery.nextid is int nextid)
                {
                    gallery = await GetUserGalleryAsync(username, nextid: nextid);
                }
                else
                {
                    yield break;
                }
            }
        }

        public async Task<WeasylSubmissionDetail> GetSubmissionAsync(int submitid)
        {
            return await GetJsonAsync<WeasylSubmissionDetail>(
                $"https://www.weasyl.com/api/submissions/{submitid}/view");
        }

        public async Task<WeasylUserProfile> GetUserAsync(string user)
        {
            return await GetJsonAsync<WeasylUserProfile>(
                $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(user)}/view");
        }

        public async Task<WeasylUserBase> WhoamiAsync()
        {
            return await GetJsonAsync<WeasylUserBase>(
                $"https://www.weasyl.com/api/whoami");
        }
    }
}
