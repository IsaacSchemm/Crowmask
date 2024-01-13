using Crowmask.Dependencies.Mapping;
using Crowmask.DomainModeling;
using Crowmask.Interfaces;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;

namespace Crowmask.Library.Feed
{
    /// <summary>
    /// Builds Atom and RSS feeds for the outbox.
    /// </summary>
    public class FeedBuilder(ActivityStreamsIdMapper mapper, ICrowmaskHost crowmaskHost, IHandleHost handleHost)
    {
        /// <summary>
        /// Generates an HTML rendition of the post, including image(s), description, and outgoing link(s).
        /// </summary>
        /// <param name="post">The submission or journal entry to render</param>
        /// <returns>A sequence of HTML strings that should be concatenated</returns>
        private static IEnumerable<string> GetHtml(Post post)
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

            yield return $"<a href='{post.url}'>View on Weasyl</a>";
        }

        /// <summary>
        /// Creates a feed item for a post.
        /// </summary>
        /// <param name="post">The submission or journal entry to render</param>
        /// <returns>A feed item</returns>
        private SyndicationItem ToSyndicationItem(Post post)
        {
            var item = new SyndicationItem
            {
                Id = mapper.GetObjectId(post.identifier),
                Title = new TextSyndicationContent(post.title, TextSyndicationContentKind.Plaintext),
                PublishDate = post.first_upstream,
                LastUpdatedTime = post.first_upstream,
                Content = new TextSyndicationContent(string.Join(" ", GetHtml(post)), TextSyndicationContentKind.Html)
            };

            item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(mapper.GetObjectId(post.identifier)), "text/html"));

            return item;
        }

        /// <summary>
        /// Creates a feed for a list of posts.
        /// </summary>
        /// <param name="person">The author of the posts</param>
        /// <param name="posts">A sequence of submissions and/or journal entries</param>
        /// <returns>A feed object</returns>
        private SyndicationFeed ToSyndicationFeed(Person person, IEnumerable<Post> posts)
        {
            string uri = $"{mapper.ActorId}/feed";
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

        /// <summary>
        /// A StringWriter that tells the XmlWriter to declare the encoding as UTF-8.
        /// </summary>
        private class UTF8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        /// <summary>
        /// Generates an RSS feed for a list of posts.
        /// </summary>
        /// <param name="person">The author of the posts</param>
        /// <param name="posts">A sequence of submissions and/or journal entries</param>
        /// <returns>An RSS feed (should be serialized as UTF-8)</returns>
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

        /// <summary>
        /// Generates an Atom feed for a list of posts.
        /// </summary>
        /// <param name="person">The author of the posts</param>
        /// <param name="posts">A sequence of submissions and/or journal entries</param>
        /// <returns>An Atom feed (should be serialized as UTF-8)</returns>
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
