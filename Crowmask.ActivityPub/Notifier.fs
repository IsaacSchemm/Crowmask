namespace Crowmask.ActivityPub

open System
open System.Net
open Crowmask.DomainModeling
open Crowmask.InteractionSummaries

type Notifier(mapper: ActivityStreamsIdMapper, summarizer: InteractionSummarizer, adminActor: IAdminActor) =
    let actor = mapper.ActorId

    let pair key value = (key, value :> obj)

    let createActivity (html: string) = dict [
        let effective_date = DateTimeOffset.UtcNow

        pair "type" "Create"
        pair "id" (mapper.GetTransientId())
        pair "to" [adminActor.Id]
        pair "actor" actor
        pair "published" effective_date

        pair "object" (dict [
            pair "id" (mapper.GetTransientId())
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

    member _.CreatePostEngagementNotification p i =
        createActivity (summarizer.ToHtml p i)
