namespace Crowmask.ActivityPub

open System
open System.Collections.Generic
open System.Text.Json

type Recipient = Followers | ActorRecipient of string

module AP =
    open Domain

    let HOST = "crowmask20231213.azurewebsites.net"
    let ACTOR = $"https://{HOST}/api/actor"

    type Object = Object of (string * obj) list

    let Context: obj list = [
        "https://w3id.org/security/v1"
        "https://www.w3.org/ns/activitystreams"
        {| sensitive = "as:sensitive" |}
    ]

    let private pair key value = (key, value :> obj)

    let Serialize apObject = JsonSerializer.Serialize(dict [   
        pair "@context" Context
        match apObject with Object list -> yield! list
    ])

    let PersonToObject (person: Person) (key: IPublicKey) = Object [
        pair "@context" Context
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

    let PersonToUpdate (person: Person) (key: IPublicKey) = Object [
        pair "@context" Context
        pair "type" "Update"
        pair "id" $"https://{HOST}/api/updates/{System.Guid.NewGuid().ToString()}"
        pair "actor" ACTOR
        pair "published" DateTimeOffset.UtcNow
        pair "object" (PersonToObject person key)
    ]

    let AsObject (note: Note) = dict [
        pair "@context" Context
        pair "id" $"https://{HOST}/api/submissions/{note.submitid}"
        pair "type" "Note"
        pair "attributedTo" ACTOR
        pair "content" note.content
        pair "published" note.published
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" $"{ACTOR}/followers"
        match note.sensitivity with
        | General -> ()
        | Sensitive warning ->
            pair "summary" warning
            pair "sensitive" true
    ]

    let AsActivity (activity: Activity) (cc: Recipient) = dict [
        pair "@context" Context
        match activity with
        | Create create ->
            pair "type" "Create"
            pair "id" $"https://{HOST}/api/creates/{create.note.submitid}"
            pair "actor" ACTOR
            pair "published" create.note.published
            pair "object" (AsObject create.note)
        | Update update ->
            pair "type" "Update"
            pair "id" $"https://{HOST}/api/updates/{System.Guid.NewGuid().ToString()}"
            pair "actor" ACTOR
            pair "published" update.time
            pair "object" (AsObject update.note)
        | Delete delete ->
            pair "type" "Delete"
            pair "id" $"https://{HOST}/api/activities/{System.Guid.NewGuid().ToString()}"
            pair "actor" ACTOR
            pair "published" delete.time
            pair "object" $"https://{HOST}/api/submissions/{delete.submitid}"

        pair "to" ["https://www.w3.org/ns/activitystreams#Public"]

        match cc with
        | Followers -> pair "cc" [$"{ACTOR}/followers"]
        | ActorRecipient actor -> pair "cc" [actor]
    ]

    let ReplaceCc (dictionary: IDictionary<string, obj>) (actor: string) = dict [
        for pair in dictionary do
            if pair.Key <> "cc" then
                pair.Key, pair.Value
        pair "cc" actor
    ]