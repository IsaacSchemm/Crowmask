namespace Crowmask.ActivityPub

open System
open System.Net
open Crowmask.DomainModeling

type Translator(mapper: ActivityStreamsIdMapper) =
    let actor = mapper.ActorId

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
                        | Some uri -> $"<a href='{uri}'>{WebUtility.HtmlEncode(metadata.value)}</a>"
                        | None -> WebUtility.HtmlEncode(metadata.value)
                |}
        ]
    ]

    member this.PersonToUpdate (person: Person) (key: IPublicKey) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject person key)
    ]

    member _.AsObject (post: Post) = dict [
        let backdate =
            post.first_cached - post.first_upstream > TimeSpan.FromHours(24)
        let effective_date =
            if backdate then post.first_upstream else post.first_cached

        let id = mapper.GetObjectId post.upstream_type

        pair "id" id
        pair "url" id
        pair "likes" $"{id}?view=likes"
        pair "shares" $"{id}?view=shares"
        pair "comments" $"{id}?view=comments"
        pair "type" (mapper.GetObjectType post.upstream_type)

        pair "attributedTo" actor
        pair "content" post.content
        pair "tag" [
            // Ensure the tag's character set matches what we expect from Weasyl, which should be OK for Mastodon too
            // If not, don't include it
            let isRestrictedSet c =
                Char.IsAscii(c)
                && (Char.IsLetterOrDigit(c) || c = '_')
                && not (Char.IsUpper(c))
            for tag in post.tags do
                if tag |> Seq.forall isRestrictedSet then
                    dict [
                        pair "type" "Hashtag"
                        pair "name" $"#{tag}"
                        pair "href" $"https://www.weasyl.com/search?q={Uri.EscapeDataString(tag)}"
                    ]
        ]
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
        pair "id" (mapper.GetTransientId())
        pair "actor" actor
        pair "published" post.first_cached
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject post)
    ]

    member this.ObjectToUpdate (post: Post) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject post)
    ]

    member _.ObjectToDelete (post: Post) = dict [
        pair "type" "Delete"
        pair "id" (mapper.GetTransientId())
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (mapper.GetObjectId post.upstream_type)
    ]

    member _.AcceptFollow (followId: string) = dict [
        pair "type" "Accept"
        pair "id" (mapper.GetTransientId())
        pair "actor" actor
        pair "object" followId
    ]

    member _.AsOutbox (gallery: Gallery) = dict [
        pair "id" $"{actor}/outbox"
        pair "type" "OrderedCollection"
        pair "totalItems" gallery.gallery_count
        pair "first" $"{actor}/outbox/page"
    ]

    member this.AsOutboxPage (id: string) (page: Page) = dict [
        pair "id" id
        pair "type" "OrderedCollectionPage"

        if page.posts <> [] then
            pair "next" $"{actor}/outbox/page?offset={page.offset + page.posts.Length}"

        pair "partOf" $"{actor}/outbox"
        pair "orderedItems" [for p in page.posts do this.AsObject p]
    ]

    member _.AsFollowersCollection (followerCollection: FollowerCollection) = dict [
        pair "id" $"{actor}/followers"
        pair "type" "Collection"
        pair "totalItems" (List.length followerCollection.followers)
        pair "items" [for f in followerCollection.followers do f.actorId]
    ]

    member _.FollowingCollection = dict [
        pair "id" $"{actor}/following"
        pair "type" "Collection"
        pair "totalItems" 0
        pair "items" []
    ]

    member _.AsLikesCollection (post: Post) = dict [
        pair "id" $"{mapper.GetObjectId post.upstream_type}?view=likes"
        pair "type" "Collection"
        pair "totalItems" (List.length post.likes)
        pair "items" [for o in post.likes do o.activity_id]
    ]

    member _.AsSharesCollection (post: Post) = dict [
        pair "id" $"{mapper.GetObjectId post.upstream_type}?view=shares"
        pair "type" "Collection"
        pair "totalItems" (List.length post.boosts)
        pair "items" [for o in post.boosts do o.activity_id]
    ]

    member _.AsCommentsCollection (post: Post) = dict [
        pair "id" $"{mapper.GetObjectId post.upstream_type}?view=comments"
        pair "type" "Collection"
        pair "totalItems" (List.length post.replies)
        pair "items" [for o in post.replies do o.object_id]
    ]
