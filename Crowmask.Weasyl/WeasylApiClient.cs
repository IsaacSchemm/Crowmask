using Microsoft.FSharp.Collections;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Crowmask.Weasyl
{
    public record WeasylWhoami(
        string login,
        int userid);

    public record WeasylMediaFile(
        int? mediaid,
        string url);

    public record WeasylUserMedia(
        FSharpList<WeasylMediaFile> avatar);

    public record WeasylStatistics(
        int submissions);

    public record WeasylUserInfo(
        int? age,
        string? gender,
        string? location,
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

    public class WeasylApiClient(IHttpClientFactory httpClientFactory, IWeasylApiKeyProvider apiKeyProvider)
    {
        private async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", "1.1"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("X-Weasyl-API-Key", apiKeyProvider.ApiKey);

            return await httpClient.GetAsync(uri, cancellationToken);
        }

        internal async Task<WeasylGallery> GetUserGalleryAsync(string username, int? count = null, int? nextid = null, int? backid = null, CancellationToken cancellationToken = default)
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

        internal async Task<WeasylSubmissionDetail?> GetSubmissionAsync(int submitid, CancellationToken cancellationToken = default)
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

        internal async Task<WeasylUserProfile> GetUserAsync(string user, CancellationToken cancellationToken = default)
        {
            using var resp = await GetAsync(
                $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(user)}/view",
                cancellationToken);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<WeasylUserProfile>(cancellationToken)
                ?? throw new NotImplementedException();
        }

        internal async Task<WeasylWhoami> WhoamiAsync(CancellationToken cancellationToken = default)
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
