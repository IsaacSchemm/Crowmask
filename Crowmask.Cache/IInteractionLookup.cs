namespace Crowmask.Cache
{
    public interface IInteractionLookup
    {
        IAsyncEnumerable<int> GetRelevantSubmitIdsAsync(string activity_or_reply_id);
        IAsyncEnumerable<int> GetRelevantJournalIdsAsync(string activity_or_reply_id);
    }
}
