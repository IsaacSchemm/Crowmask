namespace Crowmask.Dependencies.Weasyl
{
    public class WeasylUserClient(IHttpClientFactory httpClientFactory, IWeasylApiKeyProvider apiKeyProvider)
    {
        private readonly WeasylApiClient weasylClient = new(httpClientFactory, apiKeyProvider);
        private readonly WeasylScraper weasylScraper = new(httpClientFactory, apiKeyProvider);

        private WeasylWhoami? _whoami = null;
        private WeasylUserProfile? _userProfile = null;

        private async Task<WeasylWhoami> WhoamiAsync()
        {
            return _whoami ??= await weasylClient.WhoamiAsync();
        }

        public async Task<WeasylUserProfile> GetMyUserAsync()
        {
            var whoami = await WhoamiAsync();
            return _userProfile ??= await weasylClient.GetUserAsync(whoami.login);
        }

        public async Task<WeasylSubmissionDetail?> GetMyPublicSubmissionAsync(int submitid)
        {
            var whoami = await WhoamiAsync();
            var submission = await weasylClient.GetSubmissionAsync(submitid);
            return submission != null && submission.owner == whoami.login && !submission.friends_only
                ? submission
                : null;
        }

        public async Task<WeasylGallery> GetMyGalleryAsync(int? count = null, int? nextid = null, int? backid = null)
        {
            var whoami = await WhoamiAsync();
            return await weasylClient.GetUserGalleryAsync(
                username: whoami.login,
                count: count,
                nextid: nextid,
                backid: backid);
        }

        public async IAsyncEnumerable<WeasylGallerySubmission> GetMyGallerySubmissionsAsync()
        {
            var gallery = await GetMyGalleryAsync();

            while (true)
            {
                foreach (var submission in gallery.submissions)
                {
                    yield return submission;
                }

                if (gallery.nextid is int nextid)
                {
                    gallery = await GetMyGalleryAsync(nextid: nextid);
                }
                else
                {
                    yield break;
                }
            }
        }

        public async IAsyncEnumerable<int> GetMyJournalIdsAsync()
        {
            var user = await GetMyUserAsync();
            await foreach (var uri in weasylScraper.GetJournalIdsAsync(user.login_name))
                yield return uri;
        }

        public async Task<JournalEntry?> GetMyJournalAsync(int journalid)
        {
            var whoami = await WhoamiAsync();
            var journal = await weasylScraper.GetJournalAsync(journalid);
            return journal != null && journal.Username == whoami.login && !journal.VisibilityRestricted
                ? journal
                : null;
        }
    }
}
