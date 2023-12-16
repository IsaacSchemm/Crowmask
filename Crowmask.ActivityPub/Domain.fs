namespace Crowmask.ActivityPub

open System
open System.Net
open Crowmask.Data

type Image = {
    content: string
    url: string
}

type Attachment = Image of Image

type Sensitivity = General | Sensitive of warning: string

type Note = {
    submitid: int
    content: string
    published: DateTimeOffset
    attachment: Attachment list
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

module Domain =
    let AsNote (submission: Submission) =
        {
            submitid = submission.SubmitId
            content = String.concat " " [
                submission.Description
                for t in submission.Tags do
                    $"#{WebUtility.HtmlEncode(t.Tag)}"
            ]
            published = submission.PostedAt
            attachment = [
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