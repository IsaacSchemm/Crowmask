namespace Crowmask.ActivityPub

open System
open System.Net
open Crowmask.DomainModeling

type Translator(host: ICrowmaskHost) =
    let actor = $"https://{host.Hostname}/api/actor"

    let pair key value = (key, value :> obj)

    member _.PersonToObject (person: Person) (key: IPublicKey) = dict [
        pair "id" actor
        pair "type" "Person"
        pair "inbox" $"{actor}/inbox"
        pair "outbox" $"{actor}/outbox"
        pair "followers" $"{actor}/followers"
        pair "following" $"{actor}/following"
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
        pair "url" $"https://{host.Hostname}/api/submissions/{note.submitid}"
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
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
                    pair "mediaType" image.mediaType
                    pair "url" image.url
                ]
        ]
    ]

    member _.AsObject (article: Article) = dict [
        let backdate =
            article.first_cached - article.first_upstream > TimeSpan.FromHours(24)
        let effective_date =
            if backdate then article.first_upstream else article.first_cached

        pair "id" $"https://{host.Hostname}/api/journals/{article.journalid}"
        pair "type" "Article"
        pair "name" article.title
        pair "attributedTo" actor
        pair "content" article.content
        pair "published" effective_date
        pair "url" $"https://{host.Hostname}/api/journals/{article.journalid}"
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        match article.sensitivity with
        | General -> ()
        | Sensitive warning ->
            pair "summary" warning
            pair "sensitive" true
    ]

    member this.ObjectToCreate (note: Note) = dict [
        pair "type" "Create"
        pair "id" $"https://{host.Hostname}/transient/create/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" note.first_cached
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject note)
    ]

    member this.ObjectToCreate (article: Article) = dict [
        pair "type" "Create"
        pair "id" $"https://{host.Hostname}/transient/create/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" article.first_cached
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject article)
    ]

    member this.ObjectToUpdate (note: Note) = dict [
        pair "type" "Update"
        pair "id" $"https://{host.Hostname}/transient/update/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject note)
    ]

    member this.ObjectToUpdate (article: Article) = dict [
        pair "type" "Update"
        pair "id" $"https://{host.Hostname}/transient/update/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject article)
    ]

    member _.ObjectToDelete (note: Note) = dict [
        pair "type" "Delete"
        pair "id" $"https://{host.Hostname}/transient/delete/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" $"https://{host.Hostname}/api/submissions/{note.submitid}"
    ]

    member _.ObjectToDelete (article: Article) = dict [
        pair "type" "Delete"
        pair "id" $"https://{host.Hostname}/transient/delete/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" $"https://{host.Hostname}/api/journals/{article.journalid}"
    ]

    member _.AcceptFollow (followId: string) = dict [
        pair "type" "Accept"
        pair "id" $"https://{host.Hostname}/transient/accept/{Guid.NewGuid()}"
        pair "actor" actor
        pair "object" followId
    ]

    member _.AsOutbox (gallery: Gallery) = dict [
        pair "id" $"{actor}/outbox"
        pair "type" "OrderedCollection"
        pair "totalItems" gallery.gallery_count
        pair "first" $"{actor}/outbox/page"
        pair "last" $"{actor}/outbox/page?backid=1"
    ]

    member this.AsOutboxPage (id: string) (page: GalleryPage) = dict [
        pair "id" id
        pair "type" "OrderedCollectionPage"

        match page.Extrema with
        | None -> ()
        | Some extrema ->
            pair "next" $"{actor}/outbox/page?nextid={extrema.nextid}"
            pair "prev" $"{actor}/outbox/page?backid={extrema.backid}"

        pair "partOf" $"{actor}/outbox"
        pair "orderedItems" [for p in page.gallery_posts do this.AsObject p]
    ]

    member _.AsFollowers (followerCollection: FollowerCollection) = dict [
        pair "id" $"{actor}/followers"
        pair "type" "OrderedCollection"
        pair "totalItems" followerCollection.followers_count
        pair "first" $"{actor}/followers/page"
    ]

    member _.AsFollowersPage (id: string) (followerCollectionPage: FollowerCollectionPage) = dict [
        pair "id" id
        pair "type" "OrderedCollectionPage"

        match followerCollectionPage.MaxId with
        | None -> ()
        | Some maxId ->
            pair "next" $"{actor}/followers/page?after={maxId}"

        pair "partOf" $"{actor}/outbox"
        pair "orderedItems" [for f in followerCollectionPage.followers do f.actorId]
    ]

    member _.Following = dict [
        pair "id" $"{actor}/following"
        pair "type" "Collection"
        pair "totalItems" 0
        pair "items" []
    ]
