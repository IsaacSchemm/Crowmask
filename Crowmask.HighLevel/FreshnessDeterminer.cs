using Crowmask.Interfaces;

namespace Crowmask.HighLevel
{
    /// <summary>
    /// Determines whether an IPerishable post is stale or not.
    /// </summary>
    public static class FreshnessDeterminer
    {
        public static bool IsStale(IPerishable post)
        {
            var now = DateTimeOffset.UtcNow;

            bool older_than_1_hour =
                now - post.PostedAt > TimeSpan.FromHours(1);
            bool older_than_7_days =
                now - post.PostedAt > TimeSpan.FromDays(7);
            bool older_than_28_days =
                now - post.PostedAt > TimeSpan.FromDays(28);

            bool refreshed_within_1_hour =
                now - post.CacheRefreshSucceededAt < TimeSpan.FromHours(1);
            bool refreshed_within_7_days =
                now - post.CacheRefreshSucceededAt < TimeSpan.FromDays(7);
            bool refreshed_within_28_days =
                now - post.CacheRefreshSucceededAt < TimeSpan.FromDays(28);

            bool refresh_attempted_within_4_minutes =
                now - post.CacheRefreshAttemptedAt < TimeSpan.FromMinutes(4);

            if (refresh_attempted_within_4_minutes)
                return false;

            if (older_than_1_hour && refreshed_within_1_hour)
                return false;

            if (older_than_7_days && refreshed_within_7_days)
                return false;

            if (older_than_28_days && refreshed_within_28_days)
                return false;

            return true;
        }
    }
}
