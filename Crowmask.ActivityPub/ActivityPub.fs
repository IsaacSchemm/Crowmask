namespace Crowmask.ActivityPub

open System
open System.Collections.Generic
open System.Text.Json

type Recipient = Followers | ActorRecipient of string

module AP =
    open Domain

    let HOST = "crowmask20231213.azurewebsites.net"
    let ACTOR = $"https://{HOST}/api/actor"

    type Object = IDictionary<string, obj>

    let Context: obj list = [
        "https://w3id.org/security/v1"
        "https://www.w3.org/ns/activitystreams"
        {| sensitive = "as:sensitive" |}
    ]

    let private pair key value = (key, value :> obj)

    let SerializeWithContext (apObject: Object) = JsonSerializer.Serialize(dict [   
        pair "@context" Context
        for p in apObject do pair p.Key p.Value
    ])

    let PersonToObject (person: Person) (key: IPublicKey) = dict [
        pair "id" ACTOR
        pair "type" "Person"
        pair "inbox" $"{ACTOR}/inbox"
        pair "outbox" $"{ACTOR}/outbox"
        pair "followers" $"{ACTOR}/followers"
        pair "following" $"{ACTOR}/following"
        pair "preferredUsername" person.preferredUsername
        pair "name" person.name
        pair "summary" person.summary
        pair "url" person.url
        pair "publicKey" {|
            id = $"{ACTOR}#main-key"
            owner = ACTOR
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

    let PersonToUpdate (person: Person) (key: IPublicKey) = dict [
        pair "type" "Update"
        pair "id" $"https://{HOST}/api/updates/{System.Guid.NewGuid().ToString()}"
        pair "actor" ACTOR
        pair "published" DateTimeOffset.UtcNow
        pair "object" (PersonToObject person key)
    ]

    let AsObject (note: Note) = dict [
        pair "id" $"https://{HOST}/api/submissions/{note.submitid}"
        pair "type" "Note"
        pair "attributedTo" ACTOR
        pair "content" note.content
        pair "published" note.published
        pair "url" note.url
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" $"{ACTOR}/followers"
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

    let ObjectToCreate (guid: Guid) (note: Note) = dict [
        pair "type" "Create"
        pair "id" $"https://{HOST}/api/activities/{guid}"
        pair "actor" ACTOR
        pair "published" note.published
        pair "object" (AsObject note)
    ]

    let ObjectToUpdate (guid: Guid) (note: Note) = dict [
        pair "type" "Update"
        pair "id" $"https://{HOST}/api/activities/{guid}"
        pair "actor" ACTOR
        pair "published" DateTimeOffset.UtcNow
        pair "object" (AsObject note)
    ]

    let ObjectToDelete (guid: Guid) (submitid: int) = dict [
        pair "type" "Update"
        pair "id" $"https://{HOST}/api/activities/{guid}"
        pair "actor" ACTOR
        pair "published" DateTimeOffset.UtcNow
        pair "object" $"https://{HOST}/api/submissions/{submitid}"
    ]

    let AcceptFollow (guid: Guid) (followId: string) = dict [
        pair "type" "Accept"
        pair "id" $"https://{HOST}/api/activities/{guid}"
        pair "actor" ACTOR
        pair "object" followId
    ]
