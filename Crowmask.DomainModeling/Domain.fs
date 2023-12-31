﻿namespace Crowmask.DomainModeling

open System
open System.Net
open Crowmask.Data

type PersonMetadata = {
    name: string
    value: string
    uri: string option
}

type Person = {
    preferredUsername: string
    name: string
    summary: string
    url: string
    iconUrls: string list
    attachments: PersonMetadata list
}

type Image = {
    mediaType: string
    url: string
}

type Attachment = Image of Image

type Sensitivity = General | Sensitive of warning: string

type Link = {
    text: string
    href: string
}

type Boost = {
    id: Guid
    actor_id: string
    announce_id: string
    added_at: DateTimeOffset
}

type Like = {
    id: Guid
    actor_id: string
    like_id: string
    added_at: DateTimeOffset
}

type Reply = {
    id: Guid
    actor_id: string
    object_id: string
    added_at: DateTimeOffset
}

type Interaction = Boost of Boost | Like of Like | Reply of Reply
with
    member this.Id =
        match this with
        | Boost b -> b.id
        | Like l -> l.id
        | Reply r -> r.id
    member this.AddedAt =
        match this with
        | Boost b -> b.added_at
        | Like l -> l.added_at
        | Reply r -> r.added_at

[<Struct>]
type JointIdentifier =
| SubmissionIdentifier of submitid: int
| JournalIdentifier of journalid: int

type Post = {
    identifier: JointIdentifier
    title: string
    content: string
    links: Link list
    first_upstream: DateTimeOffset
    first_cached: DateTimeOffset
    attachments: Attachment list
    thumbnails: Image list
    sensitivity: Sensitivity
    tags: string list
    boosts: Boost list
    likes: Like list
    replies: Reply list
    stale: bool
} with
    member this.Interactions =
        seq {
            for i in this.boosts do Boost i
            for i in this.likes do Like i
            for r in this.replies do Reply r
        }
        |> Seq.sortBy (fun e -> e.AddedAt)
        |> Seq.toList

type CacheResult = PostResult of Post: Post | Deleted | NotFound

type Gallery = {
    gallery_count: int
}

type Page = {
    posts: Post list
    offset: int
}

type FollowerActor = {
    followerId: Guid
    actorId: string
}

type FollowerCollection = {
    followers: FollowerActor list
}

module Domain =
    let AsPerson (user: User) =
        {
            preferredUsername = user.Username
            name = user.DisplayName
            summary = user.Summary
            url = user.Url
            iconUrls = [for a in user.Avatars do a.Url]
            attachments = [
                if user.Age.HasValue then
                    {
                        name = "Age"
                        value = $"{user.Age}"
                        uri = None
                    }

                if not (String.IsNullOrWhiteSpace(user.Gender)) then
                    {
                        name = "Gender"
                        value = $"{user.Gender}"
                        uri = None
                    }

                if not (String.IsNullOrWhiteSpace(user.Location)) then
                    {
                        name = "Location"
                        value = $"{user.Location}"
                        uri = None
                    }

                for link in user.Links do
                    {
                        name = link.Site
                        value = link.UsernameOrUrl
                        uri = Option.ofObj link.Url
                    }
            ]
        }

    let AsNote (submission: Submission) =
        {
            identifier = SubmissionIdentifier submission.SubmitId
            title = submission.Title
            content = String.concat "\n" [
                submission.Content

                String.concat " " [
                    for tag in submission.Tags do
                        let href = $"https://www.weasyl.com/search?q={Uri.EscapeDataString(tag.Tag)}"
                        $"<a href='{WebUtility.HtmlEncode href}' rel='tag'>#{WebUtility.HtmlEncode tag.Tag}</a>"
                ]
            ]
            links = [
                if not (String.IsNullOrEmpty submission.Link)
                then {
                    text = "View on Weasyl"
                    href = submission.Link
                }
            ]
            first_upstream = submission.PostedAt
            first_cached = submission.FirstCachedAt
            attachments = [
                if submission.SubtypeId = Submission.Subtype.Visual then
                    for media in submission.Media do
                        Image {
                            mediaType = media.ContentType
                            url = media.Url
                        }
            ]
            thumbnails = [
                if submission.SubtypeId = Submission.Subtype.Visual then
                    for thumbnail in submission.Thumbnails do
                        {
                            mediaType = thumbnail.ContentType
                            url = thumbnail.Url
                        }
            ]
            tags = [for t in submission.Tags do t.Tag]
            sensitivity =
                match submission.RatingId with
                | Submission.Rating.General -> General
                | Submission.Rating.Mature -> Sensitive "Mature (18+)"
                | Submission.Rating.Explicit -> Sensitive "Explicit (18+)"
                | _ -> Sensitive "Potentially sensitive (nature unknown)"
            boosts = [
                for i in submission.Boosts |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        id = i.Id
                        actor_id = i.ActorId
                        announce_id = i.ActivityId
                        added_at = i.AddedAt
                    }
            ]
            likes = [
                for i in submission.Likes |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        id = i.Id
                        actor_id = i.ActorId
                        like_id = i.ActivityId
                        added_at = i.AddedAt
                    }
            ]
            replies = [
                for i in submission.Replies |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        id = i.Id
                        actor_id = i.ActorId
                        object_id = i.ObjectId
                        added_at = i.AddedAt
                    }
            ]
            stale = submission.Stale
        }

    let AsArticle (journal: Journal) =
        {
            identifier = JournalIdentifier journal.JournalId
            title = journal.Title
            content = journal.Content
            links = [
                if not (String.IsNullOrEmpty journal.Link)
                then {
                    text = "View on Weasyl"
                    href = journal.Link
                }
            ]
            first_upstream = journal.PostedAt
            first_cached = journal.FirstCachedAt
            attachments = []
            thumbnails = []
            tags = []
            sensitivity =
                match journal.Rating with
                | "General" -> General
                | str -> Sensitive str
            boosts = [
                for i in journal.Boosts |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        id = i.Id
                        actor_id = i.ActorId
                        announce_id = i.ActivityId
                        added_at = i.AddedAt
                    }
            ]
            likes = [
                for i in journal.Likes |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        id = i.Id
                        actor_id = i.ActorId
                        like_id = i.ActivityId
                        added_at = i.AddedAt
                    }
            ]
            replies = [
                for i in journal.Replies |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        id = i.Id
                        actor_id = i.ActorId
                        object_id = i.ObjectId
                        added_at = i.AddedAt
                    }
            ]
            stale = journal.Stale
        }

    let AsGallery (count: int) = {
        gallery_count = count
    }

    let AsPage (posts: Post seq, offset: int) = {
        posts = Seq.toList posts
        offset = offset
    }

    let AsFollowerActor (follower: Follower) = {
        followerId = follower.Id
        actorId = follower.ActorId
    }

    let AsFollowerCollection (followers: Follower seq) = {
        followers = [for f in followers do AsFollowerActor f]
    }
