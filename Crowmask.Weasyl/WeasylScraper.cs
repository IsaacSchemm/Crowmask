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

    public partial class WeasylScraper(IHttpClientFactory httpClientFactory, IWeasylApiKeyProvider apiKeyProvider)
    {
        private static readonly HtmlParser _htmlParser = new();

        private async Task<string> GetHtmlAsync(string uri, CancellationToken cancellationToken = default)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            httpClient.DefaultRequestHeaders.Add("X-Weasyl-API-Key", apiKeyProvider.ApiKey);

            using var resp = await httpClient.GetAsync(uri, cancellationToken);
            return await resp.Content.ReadAsStringAsync(cancellationToken);
        }

        [GeneratedRegex(@"^/journal/([0-9]+)")]
        private static partial Regex JournalUriPattern();

        public async IAsyncEnumerable<int> GetJournalIdsAsync(string login_name, [EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            string html = await GetHtmlAsync($"https://www.weasyl.com/journals/{Uri.EscapeDataString(login_name)}", cancellationToken);
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

        public async Task<JournalEntry> GetJournalAsync(int journalid, CancellationToken cancellationToken = default)
        {
            string html = await GetHtmlAsync($"https://www.weasyl.com/journal/{journalid}", cancellationToken);
            using var document = await _htmlParser.ParseDocumentAsync(html, cancellationToken);
            return new JournalEntry(
                JournalId: int.Parse(
                    document.GetElementsByTagName("input")
                    .Where(i => i.GetAttribute("name") == "journalid")
                    .Select(i => i.GetAttribute("value"))
                    .Distinct()
                    .Single()),
                Title: document.GetElementById("detail-bar-title").TextContent,
                Content: document.GetElementById("detail-journal").InnerHtml.Trim(),
                Username: document.GetElementById("detail-bar")
                    .GetElementsByClassName("username")
                    .Select(e => e.TextContent)
                    .Distinct()
                    .Single(),
                PostedAt: DateTimeOffset.Parse(
                    document.GetElementsByTagName("time")
                    .Select(e => e.GetAttribute("datetime"))
                    .Where(e => e != null)
                    .First()),
                Rating: document.GetElementsByTagName("dt")
                    .Where(dt => dt.TextContent == "Rating:")
                    .Select(dt => dt.NextElementSibling.TextContent)
                    .Single(),
                VisibilityRestricted: document.GetElementById("detail-visibility-restricted") != null);
        }
    }
}
