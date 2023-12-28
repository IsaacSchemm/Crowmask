﻿namespace Crowmask.ActivityPub

open System
open System.Net
open Crowmask.DomainModeling

type Translator(adminActor: IAdminActor, host: ICrowmaskHost) =
    let actor = $"https://{host.Hostname}/api/actor"

    let pair key value = (key, value :> obj)

    member _.PersonToObject (person: Person) (key: IPublicKey) = dict [
        pair "id" actor
        pair "type" "Person"
        pair "inbox" $"{actor}/inbox"
        pair "outbox" $"{actor}/outbox"
        pair "followers" $"{actor}/followers"
        pair "preferredUsername" person.preferredUsername
        pair "name" person.name
        pair "summary" person.summary
        pair "url" actor
        pair "publicKey" {|
            id = $"{actor}#main-key"
            owner = actor
            publicKeyPem = key.Pem
        |}
        match person.iconUrls with
        | [] -> ()
        | url::_ ->
            pair "icon" {|
                mediaType = "image/png"
                ``type`` = "Image"
                url = url
            |}
        pair "attachment" [
            for metadata in person.attachments do
                {|
                    ``type`` = "PropertyValue"
                    name = metadata.name
                    value =
                        match metadata.uri with
                        | Some uri -> $"<a href='{uri.AbsoluteUri}'>{WebUtility.HtmlEncode(metadata.value)}</a>"
                        | None -> WebUtility.HtmlEncode(metadata.value)
                |}
        ]
    ]

    member this.PersonToUpdate (person: Person) (key: IPublicKey) = dict [
        pair "type" "Update"
        pair "id" $"https://{host.Hostname}/transient/updates/{System.Guid.NewGuid().ToString()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject person key)
    ]

    member _.AsObject (note: Post) = dict [
        let backdate =
            note.first_cached - note.first_upstream > TimeSpan.FromHours(24)
        let effective_date =
            if backdate then note.first_upstream else note.first_cached

        pair "id" $"https://{host.Hostname}/api/submissions/{note.submitid}"
        pair "type" "Note"
        pair "name" note.title
        pair "attributedTo" actor
        pair "content" note.content
        pair "published" effective_date
        pair "url" $"https://{host.Hostname}/api/submissions/{note.submitid}"
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"; adminActor.Id]
        match note.sensitivity with
        | General -> ()
        | Sensitive warning ->
            pair "summary" warning
            pair "sensitive" true
        pair "attachment" [
            for attachment in note.attachments do
                match attachment with
                | Image image -> dict [
                    pair "type" "Document"
                    pair "name" $"{note.title} (from weasyl.com; no additional description available)"
                    pair "mediaType" image.mediaType
                    pair "url" image.url
                ]
        ]
    ]

    member this.ObjectToCreate (note: Post) = dict [
        pair "type" "Create"
        pair "id" $"https://{host.Hostname}/transient/create/{note.submitid}"
        pair "actor" actor
        pair "published" note.first_cached
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"; adminActor.Id]
        pair "object" (this.AsObject note)
    ]

    member this.ObjectToUpdate (note: Post) = dict [
        pair "type" "Update"
        pair "id" $"https://{host.Hostname}/transient/update/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"; adminActor.Id]
        pair "object" (this.AsObject note)
    ]

    member _.ObjectToDelete (submitid: int) = dict [
        pair "type" "Delete"
        pair "id" $"https://{host.Hostname}/transient/delete/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"; adminActor.Id]
        pair "object" $"https://{host.Hostname}/api/submissions/{submitid}"
    ]

    member _.AcceptFollow (followId: string) = dict [
        pair "type" "Accept"
        pair "id" $"https://{host.Hostname}/transient/accept/{Guid.NewGuid()}"
        pair "actor" actor
        pair "object" followId
    ]

    member _.AsOutbox (totalItems: int) = dict [
        pair "id" $"{actor}/outbox"
        pair "type" "OrderedCollection"
        pair "totalItems" totalItems
        pair "first" $"{actor}/outbox/page"
        pair "last" $"{actor}/outbox/page?backid=1"
    ]

    member this.AsOutboxPage (id: string) (posts: Post seq) = dict [
        pair "id" id
        pair "type" "OrderedCollectionPage"

        match Domain.GetExtrema posts with
        | None -> ()
        | Some extrema ->
            pair "next" $"{actor}/outbox/page?nextid={extrema.nextid}"
            pair "prev" $"{actor}/outbox/page?backid={extrema.backid}"

        pair "partOf" $"{actor}/outbox"
        pair "orderedItems" [for p in posts do this.AsObject p]
    ]

    member _.AsFollowers (totalItems: int) = dict [
        pair "id" $"{actor}/followers"
        pair "type" "OrderedCollection"
        pair "totalItems" totalItems
        pair "first" $"{actor}/followers/page"
    ]

    member _.AsFollowersPage (id: string) (followers: Crowmask.Data.Follower seq) = dict [
        pair "id" id
        pair "type" "OrderedCollectionPage"

        let ids = [for f in followers do f.Id]
        if ids <> [] then
            pair "next" $"{actor}/followers/page?after={Seq.max ids}"

        pair "partOf" $"{actor}/outbox"
        pair "orderedItems" [for f in followers do f.ActorId]
    ]
