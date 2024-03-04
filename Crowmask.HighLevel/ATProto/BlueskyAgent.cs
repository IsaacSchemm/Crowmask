using Crowmask.Data;
using Crowmask.Interfaces;
using Microsoft.EntityFrameworkCore;

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
                    await Crowmask.ATProto.Repo.deleteRecordAsync(
                        httpClient,
                        account.PDS,
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

            async IAsyncEnumerable<(byte[] data, string contentType)> downloadImagesAsync()
            {
                foreach (var image in submission.Media)
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, image.Url);
                    using var resp = await httpClient.SendAsync(req);
                    byte[] data = await resp.Content.ReadAsByteArrayAsync();
                    string mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                    yield return (data, mediaType);
                }
            }

            var allImages = await downloadImagesAsync().ToListAsync();

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
                        .SelectAwait(async image => await Crowmask.ATProto.Repo.uploadBlobAsync(
                            httpClient,
                            account.PDS,
                            wrapper,
                            image.data,
                            image.contentType))
                        .ToListAsync();
                    var post = await Crowmask.ATProto.Repo.createRecordAsync(
                        httpClient,
                        account.PDS,
                        wrapper,
                        new Crowmask.ATProto.Repo.Post(
                            text: new Textify.HtmlToTextConverter().Convert(submission.Content),
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
