namespace Crowmask.ActivityPub

open System
open System.Net
open Crowmask.DomainModeling

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

    let enc str =
        WebUtility.HtmlEncode str

    member _.CreateFollowNotification actorId actorName =
        createActivity (String.concat " " [
            $"""<a href="{enc actorId}">{enc actorName}</a>"""
            "followed this account"
        ])

    member _.CreateLikeNotification objectId title actorId actorName =
        createActivity (String.concat " " [
            $"""<a href="{enc actorId}">{enc actorName}</a>"""
            "liked the post"
            $"""<a href="{enc objectId}">{enc title}</a>"""
        ])

    member _.CreateShareNotification objectId title actorId actorName =
        createActivity (String.concat " " [
            $"""<a href="{enc actorId}">{enc actorName}</a>"""
            "shared the post"
            $"""<a href="{enc objectId}">{enc title}</a>"""
        ])

    member _.CreateReplyNotification objectId title actorId actorName replyId =
        createActivity (String.concat " " [
            $"""<a href="{enc replyId}">New reply</a>"""
            "posted by"
            $"""<a href="{enc actorId}">{enc actorName}</a>"""
            "on"
            $"""<a href="{enc objectId}">{enc title}</a>"""
        ])
