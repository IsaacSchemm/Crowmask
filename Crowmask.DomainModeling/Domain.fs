namespace Crowmask.DomainModeling

open System
open Crowmask.Data

type PersonMetadata = {
    name: string
    value: string
    uri: Uri option
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

type Note = {
    submitid: int
    title: string
    content: string
    url: string
    first_upstream: DateTimeOffset
    first_cached: DateTimeOffset
    attachments: Attachment list
    thumbnails: Image list
    sensitivity: Sensitivity
}

type Create = {
    note: Note
}

type Update = {
    note: Note
    time: DateTimeOffset
}

type Delete = {
    submitid: int
    time: DateTimeOffset
}

type Activity = Create of Create | Update of Update | Delete of Delete

type Gallery = {
    gallery_count: int
}

type GalleryPage = {
    gallery_posts: Note list
} with
    member this.Extrema =
        let ids = [for p in this.gallery_posts do p.submitid]

        if ids = []
        then None
        else Some {|
            backid = Seq.max ids
            nextid = Seq.min ids
        |}

type FollowerActor = {
    followerId: Guid
    actorId: string
}

type FollowerCollection = {
    followers_count: int
}

type FollowerCollectionPage = {
    followers: FollowerActor list
} with
    member this.MaxId =
        let ids = [for f in this.followers do f.followerId]

        if ids = []
        then None
        else Some (Seq.max ids)

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
                        uri = Option.ofObj link.Uri
                    }

                if not (isNull user.Url) then
                    {
                        name = "Weasyl"
                        value = user.Username
                        uri = Some (new Uri(user.Url))
                    }
            ]
        }

    let AsNote (submission: Submission) =
        {
            submitid = submission.SubmitId
            title = submission.Title
            content = submission.Content
            url = submission.Url
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
        }

    let AsCreate (submission: Submission) =
        Create {
            note = AsNote submission
        }

    let AsUpdate (submission: Submission) =
        Update {
            note = AsNote submission
            time = DateTimeOffset.UtcNow
        }

    let AsDelete (submitid: int) =
        Delete {
            submitid = submitid
            time = DateTimeOffset.UtcNow
        }

    let AsGallery (count: int) = {
        gallery_count = count
    }

    let AsGalleryPage (posts: Note seq) = {
        gallery_posts = Seq.toList posts
    }

    let AsFollowerActor (follower: Follower) = {
        followerId = follower.Id
        actorId = follower.ActorId
    }

    let AsFollowerCollection (count: int) = {
        followers_count = count
    }

    let AsFollowerCollectionPage (followers: Follower seq) = {
        followers = [for f in followers do AsFollowerActor f]
    }
