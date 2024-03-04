namespace Crowmask.LowLevel

open System
open Crowmask.Interfaces

/// Determines whether an IPerishable post is stale or not.
module FreshnessDeterminer =
    let BlueskyIntegrationAdded = DateTimeOffset.Parse("2024-03-04T22:15:00Z")

    let IsFresh (post: IPerishable) =
        let now = DateTimeOffset.UtcNow

        let older_than_1_hour = now - post.PostedAt > TimeSpan.FromHours(1)
        let older_than_7_days = now - post.PostedAt > TimeSpan.FromDays(7)
        let older_than_28_days = now - post.PostedAt > TimeSpan.FromDays(28)

        let refreshed_within_1_hour = now - post.CacheRefreshSucceededAt < TimeSpan.FromHours(1)
        let refreshed_within_7_days = now - post.CacheRefreshSucceededAt < TimeSpan.FromDays(7)
        let refreshed_within_28_days = now - post.CacheRefreshSucceededAt < TimeSpan.FromDays(28)

        let refresh_attempted_within_4_minutes = now - post.CacheRefreshAttemptedAt < TimeSpan.FromMinutes(4)

        let recent_enough =
            refresh_attempted_within_4_minutes
            || (older_than_1_hour && refreshed_within_1_hour)
            || (older_than_7_days && refreshed_within_7_days)
            || (older_than_28_days && refreshed_within_28_days)

        recent_enough && post.CacheRefreshSucceededAt > BlueskyIntegrationAdded

    let IsStale (post: IPerishable) =
        not (IsFresh post)
