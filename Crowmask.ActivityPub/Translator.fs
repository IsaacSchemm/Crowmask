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

    member _.AsObject (post: Post) = dict [
        let backdate =
            post.first_cached - post.first_upstream > TimeSpan.FromHours(24)
        let effective_date =
            if backdate then post.first_upstream else post.first_cached

        match post.upstream_type with
        | UpstreamSubmission submitid ->
            pair "id" $"https://{host.Hostname}/api/submissions/{submitid}"
            pair "url" $"https://{host.Hostname}/api/submissions/{submitid}"
            pair "type" "Note"
        | UpstreamJournal journalid ->
            pair "id" $"https://{host.Hostname}/api/journals/{journalid}"
            pair "url" $"https://{host.Hostname}/api/journals/{journalid}"
            pair "type" "Article"
            pair "name" post.title

        pair "attributedTo" actor
        pair "content" post.content
        pair "published" effective_date
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        match post.sensitivity with
        | General -> ()
        | Sensitive warning ->
            pair "summary" warning
            pair "sensitive" true
        pair "attachment" [
            for attachment in post.attachments do
                match attachment with
                | Image image -> dict [
                    pair "type" "Document"
                    pair "mediaType" image.mediaType
                    pair "url" image.url
                ]
        ]
    ]

    member this.ObjectToCreate (post: Post) = dict [
        pair "type" "Create"
        pair "id" $"https://{host.Hostname}/transient/create/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" post.first_cached
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject post)
    ]

    member this.ObjectToUpdate (post: Post) = dict [
        pair "type" "Update"
        pair "id" $"https://{host.Hostname}/transient/update/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject post)
    ]

    member _.ObjectToDelete (post: Post) = dict [
        pair "type" "Delete"
        pair "id" $"https://{host.Hostname}/transient/delete/{Guid.NewGuid()}"
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]

        let url =
            match post.upstream_type with
            | UpstreamSubmission submitid -> $"https://{host.Hostname}/api/submissions/{submitid}"
            | UpstreamJournal journalid -> $"https://{host.Hostname}/api/journals/{journalid}"
        pair "object" url
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

        if page.nextid.HasValue then
            pair "next" $"{actor}/outbox/page?nextid={page.nextid}"
        if page.backid.HasValue then
            pair "prev" $"{actor}/outbox/page?backid={page.backid}"

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
