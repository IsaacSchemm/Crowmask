﻿using Crowmask.Interfaces;

namespace Crowmask.Dependencies.Weasyl
{
    /// <summary>
    /// Allows access to Weasyl data using an API key.
    /// </summary>
    public class WeasylClient(ICrowmaskVersion version, IHttpClientFactory httpClientFactory, IWeasylApiKeyProvider apiKeyProvider)
    {
        private readonly WeasylApiClient weasylClient = new(version, httpClientFactory, apiKeyProvider);

        private WeasylWhoami? _whoami = null;
        private WeasylUserProfile? _userProfile = null;

        private async Task<WeasylWhoami> WhoamiAsync()
        {
            return _whoami ??= await weasylClient.WhoamiAsync();
        }

        /// <summary>
        /// Returns user profile information from the Weasyl API for the logged-in user.
        /// </summary>
        /// <returns>A user profile object</returns>
        public async Task<WeasylUserProfile> GetMyUserAsync()
        {
            var whoami = await WhoamiAsync();
            return _userProfile ??= await weasylClient.GetUserAsync(whoami.login);
        }

        /// <summary>
        /// Returns submission information for the given ID, or null if it
        /// doesn't exist, wasn't posted by the logged-in user, or is set to
        /// friends only.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <returns>A submission detail object, or null</returns>
        public async Task<WeasylSubmissionDetail?> GetMyPublicSubmissionAsync(int submitid)
        {
            var whoami = await WhoamiAsync();
            var submission = await weasylClient.GetSubmissionAsync(submitid);
            return submission != null && submission.owner == whoami.login && !submission.friends_only
                ? submission
                : null;
        }

        /// <summary>
        /// Returns a gallery page with limited submission information for
        /// multiple submissions posted by the logged-in user.
        /// </summary>
        /// <param name="count">The maximum number of submissions to include</param>
        /// <param name="nextid">The ID to use when paginating to older items</param>
        /// <param name="backid">The ID to use when paginating to newer items</param>
        /// <returns>A gallery page object</returns>
        public async Task<WeasylGallery> GetMyGalleryAsync(int? count = null, int? nextid = null, int? backid = null)
        {
            var whoami = await WhoamiAsync();
            return await weasylClient.GetUserGalleryAsync(
                username: whoami.login,
                count: count,
                nextid: nextid,
                backid: backid);
        }

        /// <summary>
        /// Returns all submissions, with limited information, for the
        /// logged-in user, newest to oldest.
        /// </summary>
        /// <returns>An asynchronous sequence of all submissions</returns>
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
    }
}