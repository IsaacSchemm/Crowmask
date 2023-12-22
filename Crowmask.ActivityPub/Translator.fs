﻿namespace Crowmask.ActivityPub

open System
open System.Collections.Generic
open System.Text.Json

open Domain

module AP =
    let Context: obj list = [
        "https://w3id.org/security/v1"
        "https://www.w3.org/ns/activitystreams"
        {| sensitive = "as:sensitive" |}
    ]

    let SerializeWithContext (apObject: IDictionary<string, obj>) = JsonSerializer.Serialize(dict [   
        "@context", Context :> obj
        for p in apObject do p.Key, p.Value
    ])

type Translator(host: ICrowmaskHost) =
    let primaryActor = $"https://{host.Hostname}/api/actor"

    let pair key value = (key, value :> obj)

    member _.PersonToObject (person: Person) (key: IPublicKey) = dict [
        pair "id" primaryActor
        pair "type" "Person"
        pair "inbox" $"{primaryActor}/inbox"
        pair "outbox" $"{primaryActor}/outbox"
        //pair "followers" $"{primaryActor}/followers"
        //pair "following" $"{primaryActor}/following"
        pair "preferredUsername" person.preferredUsername
        pair "name" person.name
        pair "summary" person.summary
        pair "url" person.url
        pair "publicKey" {|
            id = $"{primaryActor}#main-key"
            owner = primaryActor
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
            for (name, value) in person.attachments do
                {|
                    ``type`` = "PropertyValue"
                    name = name
                    value = value
                |}
        ]
    ]

    member this.PersonToUpdate (person: Person) (key: IPublicKey) = dict [
        pair "type" "Update"
        pair "id" $"https://{host.Hostname}/api/updates/{System.Guid.NewGuid().ToString()}"
        pair "actor" primaryActor
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject person key)
    ]

    member _.AsObject (note: Note) = dict [
        let backdate =
            note.first_cached - note.first_upstream > TimeSpan.FromHours(24)
        let effective_date =
            if backdate then note.first_upstream else note.first_cached

        pair "id" $"https://{host.Hostname}/api/submissions/{note.submitid}"
        pair "type" "Note"
        pair "attributedTo" primaryActor
        pair "content" note.content
        pair "published" effective_date
        pair "url" note.url
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" $"{primaryActor}/followers"
        match note.sensitivity with
        | General -> ()
        | Sensitive warning ->
            pair "summary" warning
            pair "sensitive" true
        pair "attachment" [
            for attachment in note.attachments do
                match attachment with
                | Image image ->
                    {|
                        ``type`` = "Document"
                        mediaType = "image/png" // TODO get type from Weasyl and cache it
                        name = $"{image.content} (no additional description available)"
                        url = image.url
                    |}
        ]
    ]

    member this.ObjectToCreate (guid: Guid) (note: Note) = dict [
        pair "type" "Create"
        pair "id" $"https://{host.Hostname}/api/activities/{guid}"
        pair "actor" primaryActor
        pair "published" note.first_cached
        pair "object" (this.AsObject note)
    ]

    member this.ObjectToUpdate (guid: Guid) (note: Note) = dict [
        pair "type" "Update"
        pair "id" $"https://{host.Hostname}/api/activities/{guid}"
        pair "actor" primaryActor
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.AsObject note)
    ]

    member _.ObjectToDelete (guid: Guid) (submitid: int) = dict [
        pair "type" "Delete"
        pair "id" $"https://{host.Hostname}/api/activities/{guid}"
        pair "actor" primaryActor
        pair "published" DateTimeOffset.UtcNow
        pair "object" $"https://{host.Hostname}/api/submissions/{submitid}"
    ]

    member _.AcceptFollow (guid: Guid) (followId: string) = dict [
        pair "type" "Accept"
        pair "id" $"https://{host.Hostname}/api/activities/{guid}"
        pair "actor" primaryActor
        pair "object" followId
    ]

    member this.AsOutbox (notes: IEnumerable<Note>) = dict [
        let note_list =
            notes
            |> Seq.sortByDescending (fun n -> n.first_cached)
            |> Seq.map this.AsObject
            |> Seq.toList

        pair "id" $"{primaryActor}/outbox"
        pair "type" "OrderedCollection"
        pair "totalItems" note_list.Length
        pair "orderedItems" note_list
    ]