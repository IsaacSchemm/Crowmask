using Crowmask.DomainModeling;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;

namespace Crowmask.Feed
{
    public class FeedBuilder(ICrowmaskHost crowmaskHost, IHandleHost handleHost)
    {
        private string GetUri(Post post)
        {
            string id =
                post.upstream_type is UpstreamType.UpstreamSubmission s ? $"https://{crowmaskHost.Hostname}/api/submissions/{s.submitid}"
                : post.upstream_type is UpstreamType.UpstreamJournal j ? $"https://{crowmaskHost.Hostname}/api/journals/{j.journalid}"
                : throw new NotImplementedException();

            return new(id);
        }

        private IEnumerable<string> GetHtml(Post post)
        {
            if (post.sensitivity.IsGeneral)
            {
                foreach (var attachment in post.attachments)
                    yield return $"<p><img src='{attachment.Item.url}' height='250' /></p>";
                yield return post.content;
            }
            else if (post.sensitivity is Sensitivity.Sensitive s)
            {
                yield return $"<p>{WebUtility.HtmlEncode(s.warning)}</p>";
            }

            foreach (var link in post.links)
            {
                yield return $"<a href='{link.href}'>{WebUtility.HtmlEncode(link.text)}</a>";
            }
        }

        private SyndicationItem ToSyndicationItem(Post post)
        {
            var item = new SyndicationItem
            {
                Id = GetUri(post),
                Title = new TextSyndicationContent(post.title, TextSyndicationContentKind.Plaintext),
                PublishDate = post.first_upstream,
                LastUpdatedTime = post.first_upstream,
                Content = new TextSyndicationContent(string.Join(" ", GetHtml(post)), TextSyndicationContentKind.Html)
            };

            item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(GetUri(post)), "text/html"));

            return item;
        }

        private SyndicationFeed ToSyndicationFeed(Person person, IEnumerable<Post> posts)
        {
            string uri = $"https://{crowmaskHost.Hostname}/api/actor/feed";
            var feed = new SyndicationFeed
            {
                Id = uri,
                Title = new TextSyndicationContent($"@{person.preferredUsername}@{handleHost.Hostname}", TextSyndicationContentKind.Plaintext),
                Description = new TextSyndicationContent($"Submissions and journals posted to Weasyl by {person.preferredUsername}", TextSyndicationContentKind.Plaintext),
                Copyright = new TextSyndicationContent($"{person.preferredUsername}", TextSyndicationContentKind.Plaintext),
                LastUpdatedTime = posts.Select(x => x.first_upstream).Max(),
                ImageUrl = person.iconUrls.Select(str => new Uri(str)).FirstOrDefault(),
                Items = posts.Select(ToSyndicationItem)
            };
            feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(uri), "application/rss+xml"));
            feed.Links.Add(SyndicationLink.CreateAlternateLink(new Uri($"https://{crowmaskHost.Hostname}"), "text/html"));
            return feed;
        }

        private class UTF8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        public string ToRssFeed(Person person, IEnumerable<Post> posts)
        {
            var feed = ToSyndicationFeed(person, posts);

            using var sw = new UTF8StringWriter();

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                new Rss20FeedFormatter(feed).WriteTo(xmlWriter);
            }

            return sw.ToString();
        }

        public string ToAtomFeed(Person person, IEnumerable<Post> posts)
        {
            var feed = ToSyndicationFeed(person, posts);

            using var sw = new UTF8StringWriter();

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                new Atom10FeedFormatter(feed).WriteTo(xmlWriter);
            }

            return sw.ToString();
        }
    }
}
