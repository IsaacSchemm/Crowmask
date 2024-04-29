namespace Crowmask.LowLevel

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
    alt: string
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

type PostId = SubmitId of int | JournalId of int

/// A Weasyl post to mirror to ActivityPub.
type Post = {
    id: PostId
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
        id = SubmitId submission.SubmitId
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
                    alt = submission.AltText |> Option.ofObj |> Option.defaultValue ""
                }
        ]
        thumbnails = [
            for thumbnail in submission.Thumbnails do {
                mediaType = thumbnail.ContentType
                url = thumbnail.Url
                alt = ""
            }
        ]
        tags = [for t in submission.Tags do t.Tag]
        sensitivity =
            match submission.Rating with
            | "general" -> General
            | "mature" -> Sensitive "Mature (18+)"
            | "explicit" -> Sensitive "Explicit (18+)"
            | x -> Sensitive x
        stale = FreshnessDeterminer.IsStale(submission)
    }

    let JournalAsPost(journal: Journal) = {
        id = JournalId journal.JournalId
        title = journal.Title
        content = String.concat "\n" [
            journal.Content

            if journal.Tags.Count > 0 then
                String.concat "" [
                    "<p>"
                    String.concat " " [
                        for tag in journal.Tags do
                            let href = $"https://www.weasyl.com/search?q={Uri.EscapeDataString(tag.Tag)}"
                            $"<a href='{WebUtility.HtmlEncode(href)}' rel='tag'>#{WebUtility.HtmlEncode(tag.Tag)}</a>"
                    ]
                    "</p>"
                ]
        ]
        url = journal.Link//$"https://www.weasyl.com/journal/{journal.JournalId}"
        first_upstream = journal.PostedAt
        first_cached = journal.FirstCachedAt
        images = []
        thumbnails = []
        tags = [for t in journal.Tags do t.Tag]
        sensitivity =
            match journal.Rating with
            | "general" -> General
            | "mature" -> Sensitive "Mature (18+)"
            | "explicit" -> Sensitive "Explicit (18+)"
            | x -> Sensitive x
        stale = FreshnessDeterminer.IsStale(journal)
    }

    let AsGallery(count: int) = {
        gallery_count = count
    }

    let AsGalleryPage(posts: Post seq, nextid: int) = {
        posts = Seq.toList posts
        nextid = nextid
    }

    let AsFollowerActor(follower: Follower) = {
        followerId = follower.Id
        actorId = follower.ActorId
    }

    let AsFollowerCollection(followers: Follower seq) = {
        followers = [for f in followers do AsFollowerActor f]
    }
