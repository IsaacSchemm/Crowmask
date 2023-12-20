namespace Crowmask.ActivityPub

open System
open System.Net
open Crowmask.Data

module Domain =
    type Person = {
        preferredUsername: string
        name: string
        summary: string
        url: string
        iconUrls: string list
        attachments: (string * string) list
    }

    type Image = {
        content: string
        url: string
    }

    type Attachment = Image of Image

    type Sensitivity = General | Sensitive of warning: string

    type Note = {
        submitid: int
        content: string
        url: string
        first_upstream: DateTimeOffset
        first_cached: DateTimeOffset
        attachments: Attachment list
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

    let AsPerson (user: User) =
        {
            preferredUsername = user.Username
            name = user.DisplayName
            summary = user.Summary
            url = user.Url
            iconUrls = [for a in user.Avatars do a.Url]
            attachments = [
                if user.Age.HasValue then
                    ("Age", $"{user.Age}")

                if not (String.IsNullOrWhiteSpace(user.Gender)) then
                    ("Gender", user.Gender)

                if not (String.IsNullOrWhiteSpace(user.Location)) then
                    ("Location", user.Location)

                for link in user.Links do
                    (link.Site, link.UsernameOrUrl)
            ]
        }

    let AsNote (submission: Submission) =
        {
            submitid = submission.SubmitId
            content = submission.Content
            url = submission.Url
            first_upstream = submission.PostedAt
            first_cached = submission.FirstCachedAt
            attachments = [
                if submission.SubtypeId = Submission.Subtype.Visual then
                    for media in submission.Media do
                        Image {
                            content = submission.Title
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