using AngleSharp.Html.Parser;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Crowmask.Dependencies.Weasyl
{
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
    }
}
