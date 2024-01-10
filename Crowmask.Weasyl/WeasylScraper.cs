using AngleSharp.Html.Parser;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Crowmask.Weasyl
{
    public record JournalEntry(
        int JournalId,
        string Title,
        string Content,
        string Username,
        DateTimeOffset PostedAt,
        string Rating,
        bool VisibilityRestricted);

    internal partial class WeasylScraper(IHttpClientFactory httpClientFactory, IWeasylApiKeyProvider apiKeyProvider)
    {
        private static readonly HtmlParser _htmlParser = new();

        private async Task<HttpResponseMessage> GetHtmlAsync(string uri, CancellationToken cancellationToken = default)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Crowmask", "1.1"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            httpClient.DefaultRequestHeaders.Add("X-Weasyl-API-Key", apiKeyProvider.ApiKey);

            return await httpClient.GetAsync(uri, cancellationToken);
        }

        [GeneratedRegex(@"^/journal/([0-9]+)")]
        private static partial Regex JournalUriPattern();

        public async IAsyncEnumerable<int> GetJournalIdsAsync(string login_name, [EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            using var resp = await GetHtmlAsync(
                $"https://www.weasyl.com/journals/{Uri.EscapeDataString(login_name)}",
                cancellationToken);

            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(cancellationToken);

            using var document = await _htmlParser.ParseDocumentAsync(html, cancellationToken);
            foreach (var link in document.QuerySelectorAll($"#journals-content .text-post-title a"))
            {
                if (link.GetAttribute("href") is string href)
                {
                    var match = JournalUriPattern().Match(href);
                    if (match.Success)
                    {
                        yield return int.Parse(match.Groups[1].Value);
                    }
                }
            }
        }

        public async Task<JournalEntry?> GetJournalAsync(int journalid, CancellationToken cancellationToken = default)
        {
            using var resp = await GetHtmlAsync(
                $"https://www.weasyl.com/journal/{journalid}",
                cancellationToken);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(cancellationToken);

            using var document = await _htmlParser.ParseDocumentAsync(html, cancellationToken);

            string? journalId = document.GetElementsByTagName("input")
                .Where(i => i.GetAttribute("name") == "journalid")
                .Select(i => i.GetAttribute("value"))
                .Distinct()
                .DefaultIfEmpty(null)
                .Single();
            if (journalId == null)
                return null;

            string? title = document.GetElementById("detail-bar-title")?.TextContent;
            if (title == null)
                return null;

            string? content = document.GetElementById("detail-journal")?.InnerHtml?.Trim();
            if (content == null)
                return null;

            string? username = document.QuerySelectorAll("#detail-bar")
                .SelectMany(e => e.GetElementsByClassName("username"))
                .Select(e => e.TextContent)
                .Distinct()
                .DefaultIfEmpty(null)
                .Single();
            if (username == null)
                return null;

            string? postedAt = document.GetElementsByTagName("time")
                .Select(e => e.GetAttribute("datetime"))
                .Where(e => e != null)
                .DefaultIfEmpty(null)
                .First();
            if (postedAt == null)
                return null;

            string? rating = document.GetElementsByTagName("dt")
                .Where(dt => dt.TextContent == "Rating:")
                .Select(dt => dt.NextElementSibling?.TextContent)
                .DefaultIfEmpty(null)
                .Single();
            if (rating == null)
                return null;

            return new JournalEntry(
                JournalId: int.Parse(journalId),
                Title: title,
                Content: content,
                Username: username,
                PostedAt: DateTimeOffset.Parse(postedAt),
                Rating: rating,
                VisibilityRestricted: document.GetElementById("detail-visibility-restricted") != null);
        }
    }
}
