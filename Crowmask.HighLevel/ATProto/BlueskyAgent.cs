using Crowmask.ATProto;
using Crowmask.Data;
using Crowmask.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crowmask.HighLevel.ATProto
{
    public class BlueskyAgent(
        CrowmaskDbContext context,
        IApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory)
    {
        public async Task TryDeleteBlueskyPostsAsync(Submission submission)
        {
            if (submission.BlueskyPosts.Count == 0)
                return;

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            foreach (var mirrorPost in submission.BlueskyPosts.ToList())
            {
                try
                {
                    var session = await context.BlueskySessions
                        .Where(a => a.DID == mirrorPost.DID)
                        .SingleOrDefaultAsync();

                    var account = appInfo.BlueskyBotAccounts
                        .Where(a => a.DID == mirrorPost.DID)
                        .SingleOrDefault();

                    if (session == null || account == null)
                        continue;

                    var wrapper = new TokenWrapper(context, session);
                    await Repo.DeleteRecordAsync(
                        httpClient,
                        wrapper,
                        mirrorPost.RecordKey);

                    submission.BlueskyPosts.Remove(mirrorPost);
                }
                catch (Exception) { }
            }
        }

        public async Task TryCreateBlueskyPostsAsync(Submission submission)
        {
            if (!appInfo.BlueskyBotAccounts.Any())
                return;

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            async IAsyncEnumerable<(byte[] data, string contentType, string alt)> downloadImagesAsync()
            {
                foreach (var image in submission.Media)
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, image.Url);
                    using var resp = await httpClient.SendAsync(req);
                    byte[] data = await resp.Content.ReadAsByteArrayAsync();
                    string mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                    yield return (data, mediaType, image.AltText);
                }
            }

            var allImages = await downloadImagesAsync().ToListAsync();

            var converter = new Textify.HtmlToTextConverter();

            foreach (var account in appInfo.BlueskyBotAccounts)
            {
                try
                {
                    if (submission.BlueskyPosts.Any(m => m.DID == account.DID))
                        continue;

                    var session = await context.BlueskySessions
                        .Where(a => a.DID == account.DID)
                        .SingleOrDefaultAsync();

                    if (session == null)
                        continue;

                    var wrapper = new TokenWrapper(context, session);
                    var blobResponses = await allImages
                        .ToAsyncEnumerable()
                        .SelectAwait(async image => await Repo.UploadBlobAsync(
                            httpClient,
                            wrapper,
                            image.data,
                            image.contentType,
                            image.alt))
                        .ToListAsync();
                    var post = await Repo.CreateRecordAsync(
                        httpClient,
                        wrapper,
                        new Repo.Post(
                            text: converter.Convert(submission.Content),
                            createdAt: submission.PostedAt,
                            images: blobResponses));

                    submission.BlueskyPosts.Add(new Submission.BlueskyPost
                    {
                        DID = account.DID,
                        RecordKey = post.RecordKey
                    });
                }
                catch (Exception) { }
            }
        }
    }
}
