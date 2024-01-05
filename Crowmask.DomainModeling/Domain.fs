namespace Crowmask.DomainModeling

open System
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

type Interaction = {
    actor_id: string
    activity_id: string
    added_at: DateTimeOffset
}

type Reply = {
    actor_id: string
    object_id: string
    added_at: DateTimeOffset
}

type UpstreamType =
| UpstreamSubmission of submitid: int
| UpstreamJournal of journalid: int

type Post = {
    upstream_type: UpstreamType
    title: string
    content: string
    links: Link list
    first_upstream: DateTimeOffset
    first_cached: DateTimeOffset
    attachments: Attachment list
    thumbnails: Image list
    sensitivity: Sensitivity
    boosts: Interaction list
    likes: Interaction list
    replies: Reply list
    stale: bool
}

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
            upstream_type = UpstreamSubmission submission.SubmitId
            title = submission.Title
            content = submission.Content
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
            sensitivity =
                match submission.RatingId with
                | Submission.Rating.General -> General
                | Submission.Rating.Mature -> Sensitive "Mature (18+)"
                | Submission.Rating.Explicit -> Sensitive "Explicit (18+)"
                | _ -> Sensitive "Potentially sensitive (nature unknown)"
            boosts = [
                for i in submission.Boosts |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        actor_id = i.ActorId
                        activity_id = i.ActivityId
                        added_at = i.AddedAt
                    }
            ]
            likes = [
                for i in submission.Likes |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        actor_id = i.ActorId
                        activity_id = i.ActivityId
                        added_at = i.AddedAt
                    }
            ]
            replies = [
                for i in submission.Replies |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        actor_id = i.ActorId
                        object_id = i.ObjectId
                        added_at = i.AddedAt
                    }
            ]
            stale = submission.Stale
        }

    let AsArticle (journal: Journal) =
        {
            upstream_type = UpstreamJournal journal.JournalId
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
            sensitivity =
                match journal.Rating with
                | "General" -> General
                | str -> Sensitive str
            boosts = [
                for i in journal.Boosts |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        actor_id = i.ActorId
                        activity_id = i.ActivityId
                        added_at = i.AddedAt
                    }
            ]
            likes = [
                for i in journal.Likes |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
                        actor_id = i.ActorId
                        activity_id = i.ActivityId
                        added_at = i.AddedAt
                    }
            ]
            replies = [
                for i in journal.Replies |> Seq.sortBy (fun x -> x.AddedAt) do
                    {
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
