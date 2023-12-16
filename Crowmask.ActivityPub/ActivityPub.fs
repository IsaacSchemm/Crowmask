namespace Crowmask.ActivityPub

open System.Collections.Generic

type Recipient = Followers | ActorRecipient of string

module AP =
    let HOST = "crowmask20231213.azurewebsites.net"
    let ACTOR = $"https://{HOST}/api/actor"

    let private pair key value = (key, value :> obj)

    let AsObject (note: Note) = dict [
        pair "@context" [
            "https://www.w3.org/ns/activitystreams"
            "https://w3id.org/security/v1"
        ]
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
        pair "@context" [
            "https://www.w3.org/ns/activitystreams"
            "https://w3id.org/security/v1"
        ]
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