using Crowmask.Cache;
using Crowmask.DomainModeling;
using Crowmask.Merging;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Crowmask.Functions
{
    public class Feed(CrowmaskCache crowmaskCache, ICrowmaskHost crowmaskHost, IHandleHost handleHost, WeasylUserClient weasylUserClient)
    {
        private SyndicationItem ToSyndicationItem(Post post)
        {
            string path =
                post.upstream_type is UpstreamType.UpstreamSubmission s ? $"api/submissions/{s.submitid}"
                : post.upstream_type is UpstreamType.UpstreamJournal j ? $"api/journals/{j.journalid}"
                : throw new NotImplementedException();
            Uri uri = new($"https://{crowmaskHost.Hostname}/{path}");

            StringBuilder sb = new();
            if (post.sensitivity.IsGeneral)
            {
                foreach (var attachment in post.attachments)
                {
                    sb.Append($"<p><img src='{attachment.Item.url}' height='250' /></p>");
                }
            }
            sb.Append(post.content);

            var item = new SyndicationItem
            {
                Id = uri.AbsoluteUri,
                Title = new TextSyndicationContent(post.title, TextSyndicationContentKind.Plaintext),
                PublishDate = post.first_upstream,
                LastUpdatedTime = post.first_upstream,
                Content = new TextSyndicationContent(sb.ToString(), TextSyndicationContentKind.Html)
            };
            item.Links.Add(SyndicationLink.CreateAlternateLink(uri, "text/html"));
            return item;
        }

        [Function("Feed")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor/feed")] HttpRequestData req)
        {
            var posts =
                await new[] {
                    crowmaskCache.GetCachedSubmissionsAsync(),
                    crowmaskCache.GetCachedJournalsAsync()
                }
                .MergeNewest(post => post.first_upstream)
                .Take(20)
                .ToListAsync();

            var person = await crowmaskCache.UpdateUserAsync();

            var feed = new SyndicationFeed
            {
                Id = req.Url.AbsoluteUri,
                Title = new TextSyndicationContent($"@{person.preferredUsername}@{handleHost.Hostname} (Crowmask)", TextSyndicationContentKind.Plaintext),
                Description = new TextSyndicationContent($"A mirror of submissions and journals posted to Weasyl by {person.preferredUsername}", TextSyndicationContentKind.Plaintext),
                Copyright = new TextSyndicationContent($"{person.preferredUsername}", TextSyndicationContentKind.Plaintext),
                LastUpdatedTime = posts.Select(x => x.first_upstream).Max(),
                ImageUrl = person.iconUrls.Select(str => new Uri(str)).FirstOrDefault(),
                Items = posts.Select(ToSyndicationItem)
            };
            feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(req.Url, "application/rss+xml")));
            feed.Links.Add(SyndicationLink.CreateAlternateLink(new Uri($"https://{crowmaskHost.Hostname}"), "text/html"));

            using var ms = new MemoryStream();
            using (var xmlWriter = XmlWriter.Create(ms))
            {
                new Rss20FeedFormatter(feed).WriteTo(xmlWriter);
            }

            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", "application/rss+xml");
            await resp.WriteBytesAsync(ms.ToArray());
            return resp;
        }
    }
}
