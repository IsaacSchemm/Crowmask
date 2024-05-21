namespace Crowmask.Data
{
    /// <summary>
    /// An item that is only considered "fresh" for a certain amount of time, based on its initial post date and last refresh date.
    /// </summary>
    public interface IPerishable
    {
        /// <summary>
        /// The date and time at which the item was originally posted to Weasyl.
        /// </summary>
        DateTimeOffset PostedAt { get; }

        /// <summary>
        /// The last time Crowmask attempted to refresh this item.
        /// </summary>
        DateTimeOffset CacheRefreshAttemptedAt { get; }

        /// <summary>
        /// The last time Crowmask successfully refreshed this item.
        /// </summary>
        DateTimeOffset CacheRefreshSucceededAt { get; }
    }
}
