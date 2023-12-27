namespace Crowmask.ActivityPub

open System
open System.Net

type Notifier(adminActor: IAdminActor, host: ICrowmaskHost) =
    let actor = $"https://{host.Hostname}/api/actor"

    let pair key value = (key, value :> obj)

    let createActivity (html: string) = dict [
        let effective_date = DateTimeOffset.UtcNow

        pair "type" "Create"
        pair "id" $"https://{host.Hostname}/transient/create/{Guid.NewGuid()}"
        pair "to" [adminActor.Id]
        pair "actor" actor
        pair "published" effective_date

        pair "object" (dict [
            pair "id" $"https://{host.Hostname}/transient/note/{Guid.NewGuid()}"
            pair "type" "Note"
            pair "attributedTo" actor
            pair "content" html
            pair "published" effective_date
            pair "to" [adminActor.Id]
        ])
    ]

    let objectIdFor (submission: Crowmask.Data.Submission) =
        $"https://{host.Hostname}/api/submissions/{submission.SubmitId}"

    let enc str =
        WebUtility.HtmlEncode str

    member _.CreateLikeNotification submission actorId actorName =
        createActivity (String.concat " " [
            $"""<a href="{enc actorId}">{enc actorName}</a>"""
            "liked the post"
            $"""<a href="{objectIdFor submission}">{enc submission.Title}</a>"""
        ])

    member _.CreateShareNotification submission actorId actorName =
        createActivity (String.concat " " [
            $"""<a href="{enc actorId}">{enc actorName}</a>"""
            "shared the post"
            $"""<a href="{objectIdFor submission}">{enc submission.Title}</a>"""
        ])

    member _.CreateReplyNotification submission actorId actorName replyId =
        createActivity (String.concat " " [
            $"""<a href="{enc replyId}">New reply</a>"""
            "posted by"
            $"""<a href="{enc actorId}">{enc actorName}</a>"""
            "on"
            $"""<a href="{objectIdFor submission}">{enc submission.Title}</a>"""
        ])
