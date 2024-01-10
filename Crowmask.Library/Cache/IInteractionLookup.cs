namespace Crowmask.Library.Cache
{
    /// <summary>
    /// Provides a way to quickly look up a Crowmask post, given the ID of a
    /// relevant ActivityPub object (a Like, an Announce, or a reply). Allows
    /// for dependency inversion.
    /// </summary>
    public interface IInteractionLookup
    {
        /// <summary>
        /// Given the ID of a relevant ActivityPub object, yields the
        /// corresponding submission ID, or yields no items if the activity or
        /// object is not present on any submissions in Crowmask's cache.
        /// </summary>
        /// <param name="activity_or_reply_id">The ID of an Announce or Like activity, or the ID of a reply to an artwork submission</param>
        /// <returns>All relevant submission IDs (expected to be zero or one items)</returns>
        IAsyncEnumerable<int> GetRelevantSubmitIdsAsync(string activity_or_reply_id);

        /// <summary>
        /// Given the ID of a relevant ActivityPub object, yields the
        /// corresponding journal ID, or yields no items if the activity or
        /// object is not present on any journals in Crowmask's cache.
        /// </summary>
        /// <param name="activity_or_reply_id">The ID of an Announce or Like activity, or the ID of a reply to a journal</param>
        /// <returns>All relevant journal IDs (expected to be zero or one items)</returns>
        IAsyncEnumerable<int> GetRelevantJournalIdsAsync(string activity_or_reply_id);
    }
}
