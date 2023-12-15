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
    activityid: Guid
    note: Note
}

type Update = {
    activityid: Guid
    note: Note
    time: DateTimeOffset
}

type Delete = {
    activityid: Guid
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
            activityid = submission.Id
            note = AsNote submission
        }

    let AsUpdate (submission: Submission, activity: UpdateActivity) =
        Update {
            activityid = activity.Id
            note = AsNote submission
            time = activity.PublishedAt
        }

    let AsDelete (activity: DeleteActivity) =
        Delete {
            activityid = activity.Id
            submitid = activity.SubmitId
            time = activity.PublishedAt
        }