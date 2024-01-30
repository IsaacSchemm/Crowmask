﻿namespace Crowmask.LowLevel

open System
open System.Net
open Crowmask.Data

/// A name/value pair (with optional link) that can appear on a user profile.
type PersonMetadata = {
    name: string
    value: string
    uri: string option
}

/// A user profile; in particular, the one created by this Crowmask instance.
type Person = {
    upstreamUsername: string
    name: string
    summary: string
    url: string
    iconUrls: string list
    attachments: PersonMetadata list
}

/// An image with a known media type.
type Image = {
    mediaType: string
    url: string
}

/// A sensitivity level for a post - either general, or with a specific warning.
type Sensitivity = General | Sensitive of warning: string

/// A link to display alongside the post in Crowmask's HTML interface.
type Link = {
    text: string
    href: string
}

/// An activity or object sent to Crowmask's inbox by another user.
type RemoteAction = {
    id: Guid
    actor_id: string
    added_at: DateTimeOffset
}

/// A boost or like (Announce or Like activity) from another user.
type RemoteInteraction = {
    id: Guid
    actor_id: string
    activity_id: string
    activity_type: string
    target_id: string
    added_at: DateTimeOffset
}

/// A mention or reply from another user.
type RemoteMention = {
    id: Guid
    actor_id: string
    object_id: string
    added_at: DateTimeOffset
}

/// A Weasyl post to mirror to ActivityPub.
type Post = {
    submitid: int
    title: string
    content: string
    url: string
    first_upstream: DateTimeOffset
    first_cached: DateTimeOffset
    images: Image list
    thumbnails: Image list
    sensitivity: Sensitivity
    tags: string list
    stale: bool
}

/// A result from a request to Crowmask's database cache looking for a post.
/// C# consumers can perform a type check with the "is" operator to retrieve post information.
type CacheResult = PostResult of Post: Post | PostNotFound

/// The main page of the user's gallery (not the first page).
type Gallery = {
    gallery_count: int
}

/// A single page of the user's gallery.
type GalleryPage = {
    posts: Post list
    nextid: int
}

/// An ActivityPub actor who is following this actor.
type FollowerActor = {
    followerId: Guid
    actorId: string
}

/// A list of ActivityPub actors who are following this actor.
type FollowerCollection = {
    followers: FollowerActor list
}

module Domain =
    let AsPerson(user: User) = {
        upstreamUsername = user.Username
        name = user.DisplayName
        summary = user.Summary
        url = user.Url
        iconUrls = [for a in user.Avatars do a.Url]
        attachments = [
            let add n v u = {
                name = n
                value = string v
                uri = u
            }

            if user.Age.HasValue then
                add "Age" user.Age None
            if not (String.IsNullOrWhiteSpace(user.Gender)) then
                add "Gender" user.Gender None
            if not (String.IsNullOrWhiteSpace(user.Location)) then
                add "Location" user.Location None

            for link in user.Links do
                add link.Site link.UsernameOrUrl (Option.ofObj link.Url)

            add "Weasyl" user.Username (Option.ofObj user.Url)
        ]
    }

    let AsPost(submission: Submission) = {
        submitid = submission.SubmitId
        title = submission.Title
        content = String.concat "\n" [
            if submission.Visual then
                $"<p><b>{WebUtility.HtmlEncode(submission.Title)}</b></p>"
            else
                $"<p><a href='{WebUtility.HtmlEncode(submission.Link)}'><b>{WebUtility.HtmlEncode(submission.Title)}</b></a></p>"

            submission.Content

            if submission.Tags.Count > 0 then
                String.concat "" [
                    "<p>"
                    String.concat " " [
                        for tag in submission.Tags do
                            let href = $"https://www.weasyl.com/search?q={Uri.EscapeDataString(tag.Tag)}"
                            $"<a href='{WebUtility.HtmlEncode(href)}' rel='tag'>#{WebUtility.HtmlEncode(tag.Tag)}</a>"
                    ]
                    "</p>"
                ]
        ]
        url = submission.Link
        first_upstream = submission.PostedAt
        first_cached = submission.FirstCachedAt
        images = [
            if submission.Visual then
                for media in submission.Media do {
                    mediaType = media.ContentType
                    url = media.Url
                }
        ]
        thumbnails = [
            for thumbnail in submission.Thumbnails do {
                mediaType = thumbnail.ContentType
                url = thumbnail.Url
            }
        ]
        tags = [for t in submission.Tags do t.Tag]
        sensitivity =
            match submission.Rating with
            | "general" -> General
            | "mature" -> Sensitive "Mature (18+)"
            | "explicit" -> Sensitive "Explicit (18+)"
            | x -> Sensitive x
        stale = submission.Stale
    }

    let AsGallery(count: int) = {
        gallery_count = count
    }

    let AsGalleryPage(posts: Post seq, nextid: int) = {
        posts = Seq.toList posts
        nextid = nextid
    }

    let AsRemoteInteraction(interaction: Interaction) = {
        id = interaction.Id
        actor_id = interaction.ActorId
        activity_id = interaction.ActivityId
        activity_type = interaction.ActivityType
        added_at = interaction.AddedAt
        target_id = interaction.TargetId
    }

    let AsRemoteMention(mention: Mention) = {
        id = mention.Id
        actor_id = mention.ActorId
        added_at = mention.AddedAt
        object_id = mention.ObjectId
    }

    let AsFollowerActor(follower: Follower) = {
        followerId = follower.Id
        actorId = follower.ActorId
    }

    let AsFollowerCollection(followers: Follower seq) = {
        followers = [for f in followers do AsFollowerActor f]
    }
