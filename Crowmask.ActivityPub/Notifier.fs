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

    let enc =
        WebUtility.HtmlEncode

    member _.CreateLikeNotification (submission: Crowmask.Data.Submission) (other_actor: IRemoteActorDisplay) =
        createActivity (String.concat " " [
            $"""<a href="{enc other_actor.Id}">{enc other_actor.DisplayName}</a>"""
            "liked the post"
            $"""<a href="{objectIdFor submission}">{enc submission.Title}</a>"""
        ])

    member _.CreateShareNotification (submission: Crowmask.Data.Submission) (other_actor: IRemoteActorDisplay) =
        createActivity (String.concat " " [
            $"""<a href="{enc other_actor.Id}">{enc other_actor.DisplayName}</a>"""
            "shared the post"
            $"""<a href="{objectIdFor submission}">{enc submission.Title}</a>"""
        ])

    member _.CreateReplyNotification (submission: Crowmask.Data.Submission) (other_actor: IRemoteActorDisplay) (reply_object_id: string) =
        createActivity (String.concat " " [
            $"""<a href="{enc reply_object_id}">{enc other_actor.DisplayName} replied</a>"""
            "to the post"
            $"""<a href="{objectIdFor submission}">{enc submission.Title}</a>"""
        ])
