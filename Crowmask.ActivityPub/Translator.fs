namespace Crowmask.ActivityPub

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

type Translator(adminActor: IAdminActor, host: ICrowmaskHost) =
    let actor = $"https://{host.Hostname}/api/actor"

    let pair key value = (key, value :> obj)

    member _.PersonToObject (person: Person) (key: IPublicKey) = dict [
        pair "id" actor
        pair "type" "Person"
        pair "inbox" $"{actor}/inbox"
        pair "outbox" $"{actor}/outbox"
        pair "preferredUsername" person.preferredUsername
        pair "name" person.name
        pair "summary" person.summary
        pair "url" person.url
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
        pair "id" $"https://{host.Hostname}/transient/updates/{System.Guid.NewGuid().ToString()}"
        pair "actor" actor
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
        pair "attributedTo" actor
        pair "content" note.content
        pair "published" effective_date
        pair "url" $"https://{host.Hostname}/api/submissions/{note.submitid}" //note.url
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
                | Image image ->
                    {|
                        ``type`` = "Document"
                        mediaType = "image/png" // TODO get type from Weasyl and cache it
                        name = $"{image.content} (no additional description available)"
                        url = image.url
                    |}
        ]
    ]

    member this.ObjectToCreate (note: Note) = dict [
        pair "type" "Create"
        pair "id" $"https://{host.Hostname}/api/creations/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" note.first_cached
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"; adminActor.Id]
        pair "object" (this.AsObject note)
    ]

    member this.ObjectToUpdate (note: Note) = dict [
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

    member this.AsOutbox (notes: IEnumerable<Note>) = dict [
        let note_list =
            notes
            |> Seq.sortByDescending (fun n -> n.first_cached)
            |> Seq.map this.ObjectToCreate
            |> Seq.toList

        pair "id" $"{actor}/outbox"
        pair "type" "OrderedCollection"
        pair "totalItems" note_list.Length
        pair "orderedItems" note_list
    ]
