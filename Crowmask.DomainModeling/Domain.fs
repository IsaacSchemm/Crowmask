namespace Crowmask.DomainModeling

open System
open MimeMapping
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

type Post = {
    submitid: int
    title: string
    content: string
    url: string
    first_upstream: DateTimeOffset
    first_cached: DateTimeOffset
    attachments: Attachment list
    sensitivity: Sensitivity
}

type Create = {
    note: Post
}

type Update = {
    note: Post
    time: DateTimeOffset
}

type Delete = {
    submitid: int
    time: DateTimeOffset
}

type Activity = Create of Create | Update of Update | Delete of Delete

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
                            mediaType =
                                (new Uri(media.Url)).Segments
                                |> Array.last
                                |> MimeUtility.GetMimeMapping
                            url = media.Url
                        }
            ]
            sensitivity =
                match submission.RatingId with
                | Submission.Rating.General -> General
                | Submission.Rating.Mature -> Sensitive "Mature (18+)"
                | Submission.Rating.Explicit -> Sensitive "Explicit (18+)"
                | _ -> Sensitive "Potentially sensitive (nature unknown)"
        }

    let GetExtrema (posts: Post seq) =
        let ids = [for p in posts do p.submitid]

        if ids = []
        then None
        else Some {|
            backid = Seq.max ids
            nextid = Seq.min ids
        |}

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