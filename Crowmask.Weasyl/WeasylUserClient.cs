namespace Crowmask.Weasyl
{
    public class WeasylUserClient(WeasylBaseClient weasylClient, WeasylScraper weasylScraper)
    {
        private WeasylWhoami _whoami = null;
        private WeasylUserProfile _userProfile = null;

        private async Task<WeasylWhoami> WhoamiAsync()
        {
            return _whoami ??= await weasylClient.WhoamiAsync();
        }

        public async Task<WeasylUserProfile> GetMyUserAsync()
        {
            var whoami = await WhoamiAsync();
            return _userProfile ??= await weasylClient.GetUserAsync(whoami.login);
        }

        public async Task<WeasylSubmissionDetail> GetMyPublicSubmissionAsync(int submitid)
        {
            try
            {
                var whoami = await WhoamiAsync();
                var submission = await weasylClient.GetSubmissionAsync(submitid);
                return submission.owner == whoami.login && !submission.friends_only
                    ? submission
                    : null;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
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

        public async Task<JournalEntry> GetMyJournalAsync(int journalid)
        {
            var whoami = await WhoamiAsync();
            try
            {
                var journal = await weasylScraper.GetJournalAsync(journalid);
                return journal.Username == whoami.login && !journal.VisibilityRestricted
                    ? journal
                    : null;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
