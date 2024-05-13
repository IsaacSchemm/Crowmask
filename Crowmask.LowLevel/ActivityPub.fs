namespace Crowmask.LowLevel

open System
open System.Collections.Generic
open System.Net
open System.Text.Json
open Crowmask.Interfaces
open Crowmask.Data

/// Contains functions for JSON-LD serialization.
module ActivityPubSerializer =
    /// A JSON-LD context that includes all fields used by Crowmask.
    let Context: obj list = [
        "https://w3id.org/security/v1"
        "https://www.w3.org/ns/activitystreams"

        {| 
            // https://docs.joinmastodon.org/spec/activitypub/#as
            Hashtag = "as:Hashtag"
            sensitive = "as:sensitive"

            toot = "http://joinmastodon.org/ns#"
            discoverable = "toot:discoverable"
            indexable = "toot:indexable"
        |}
    ]

    /// Converts ActivityPub objects in string/object pair format to an
    /// acceptable JSON-LD rendition.
    let SerializeWithContext (apObject: IDictionary<string, obj>) = JsonSerializer.Serialize(dict [   
        "@context", Context :> obj
        for p in apObject do p.Key, p.Value
    ])

/// Creates ActivityPub objects (in string/object pair format) for actors,
/// posts, and other objects tracked by Crowmask.
type ActivityPubTranslator(appInfo: IApplicationInformation, summarizer: Summarizer, mapper: IdMapper) =
    /// The Crowmask actor ID.
    let actor = mapper.ActorId

    /// Creates a string/object pair (F# tuple) with the given key and value.
    let pair key value = (key, value :> obj)

    /// Checks whether the character is in the set that Weasyl allows for
    /// tags, which is a subset of what Mastodon allows.
    let isRestrictedSet c =
        Char.IsAscii(c)
        && (Char.IsLetterOrDigit(c) || c = '_')
        && not (Char.IsUpper(c))

    /// Builds a Person object for the Crowmask actor.
    member _.PersonToObject (person: Person) (key: IActorKey) (appInfo: IApplicationInformation) = dict [
        pair "id" actor
        pair "type" "Person"
        pair "inbox" $"{actor}/inbox"
        pair "outbox" $"{actor}/outbox"
        pair "followers" $"{actor}/followers"
        pair "following" $"{actor}/following"
        pair "preferredUsername" appInfo.Username
        pair "name" person.name
        pair "summary" person.summary
        pair "url" actor
        pair "discoverable" true
        pair "indexable" true
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
            for metadata in person.attachments do {|
                ``type`` = "PropertyValue"
                name = metadata.name
                value =
                    match metadata.uri with
                    | Some uri -> $"<a href='{uri}'>{WebUtility.HtmlEncode(metadata.value)}</a>"
                    | None -> WebUtility.HtmlEncode(metadata.value)
            |}

            {|
                ``type`` = "PropertyValue"
                name = "Mirrored by"
                value = $"<a href='{appInfo.WebsiteUrl}'>{WebUtility.HtmlEncode(appInfo.ApplicationName)}</a> (APGLv3)"
            |}
        ]
    ]

    /// Builds a transient Update activity for the Crowmask actor.
    member this.PersonToUpdate (person: Person) (key: IActorKey) = dict [
        pair "type" "Update"
        pair "id" (mapper.GenerateTransientId())
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject person key)
    ]

    /// Builds a Note object for a submission.
    member _.AsObject (post: Post) = dict [
        let backdate =
            post.first_cached - post.first_upstream > TimeSpan.FromHours(24)
        let effective_date =
            if backdate then post.first_upstream else post.first_cached

        let id = mapper.GetObjectId(post.id)

        pair "id" id
        pair "url" id

        match post.id with
        | SubmitId _ ->
            pair "type" "Note"
        | JournalId _ ->
            pair "type" "Article"
            pair "name" post.title

        pair "attributedTo" actor
        pair "content" post.content
        pair "tag" [
            for tag in post.tags do
                // Skip the tag if it doesn't meet our character set expectations.
                // Weasyl doesn't allow much so no need to do normalization ourselves.
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
            for image in post.images do dict [
                pair "type" "Document"
                pair "mediaType" image.mediaType
                pair "url" image.url
                if image.alt <> "" then
                    pair "name" image.alt
            ]
        ]
    ]

    /// Builds a Create activity for a submission.
    member this.ObjectToCreate (post: Post) = dict [
        pair "type" "Create"
        pair "id" $"{mapper.GetObjectId(post.id)}?view=create"
        pair "actor" actor
        pair "published" post.first_cached
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject post)
    ]

    /// Builds a transient Update activity for a submission.
    member this.ObjectToUpdate (post: Post) = dict [
        pair "type" "Update"
        pair "id" (mapper.GenerateTransientId())
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (this.AsObject post)
    ]

    /// Builds a transient Delete activity for a submission.
    member _.ObjectToDelete (post: Post) = dict [
        pair "type" "Delete"
        pair "id" (mapper.GenerateTransientId())
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [$"{actor}/followers"]
        pair "object" (mapper.GetObjectId(post.id))
    ]

    /// Builds a transient Create activity for a transient Note sent to the admin actor.
    member _.CreateTransientPrivateNote (markdown_content: string) = dict [
        pair "type" "Create"
        pair "id" (mapper.GenerateTransientId())
        pair "actor" actor
        pair "published" DateTimeOffset.UtcNow
        pair "to" [yield! appInfo.AdminActorIds]
        pair "object" (dict [
            let id = (mapper.GenerateTransientId())

            pair "id" id
            pair "url" id
            pair "type" "Note"

            pair "attributedTo" actor
            pair "content" (Markdig.Markdown.ToHtml markdown_content)
            pair "published" DateTimeOffset.UtcNow
            pair "to" [yield! appInfo.AdminActorIds]
        ])
    ]

    /// Builds a transient Create activity for a transient Note sent to the admin actor.
    member this.CreateTransientPrivateNote (interaction: Interaction) =
        this.CreateTransientPrivateNote (summarizer.ToMarkdown interaction)

    /// Builds a transient Create activity for a transient Note sent to the admin actor.
    member this.CreateTransientPrivateNote (mention: Mention) =
        this.CreateTransientPrivateNote (summarizer.ToMarkdown mention)

    /// Builds a transient Accept activity to accept a follow request.
    member _.AcceptFollow (followId: string) = dict [
        pair "type" "Accept"
        pair "id" (mapper.GenerateTransientId())
        pair "actor" actor
        pair "object" followId
    ]

    /// Builds an OrderedCollection to represent the user's outbox.
    member _.AsOutbox (gallery: Gallery) = dict [
        pair "id" $"{actor}/outbox"
        pair "type" "OrderedCollection"
        pair "totalItems" gallery.gallery_count
        pair "first" $"{actor}/outbox/page"
    ]

    /// Builds an OrderedCollectionPage to represent a single page of the user's outbox.
    member this.AsOutboxPage (id: string) (page: GalleryPage) = dict [
        pair "id" id
        pair "type" "OrderedCollectionPage"

        if page.posts <> [] then
            pair "next" (mapper.GetNextOutboxPage [for p in page.posts do p.id])

        pair "partOf" $"{actor}/outbox"
        pair "orderedItems" [for p in page.posts do this.ObjectToCreate p]
    ]

    /// Builds a Collection to list the user's followers.
    member _.AsFollowersCollection (followerCollection: FollowerCollection) = dict [
        pair "id" $"{actor}/followers"
        pair "type" "Collection"
        pair "totalItems" (List.length followerCollection.followers)
        pair "items" [for f in followerCollection.followers do f.actorId]
    ]

    /// An empty Collection to show that the user is not following any other
    /// ActivityPub actors (following other ActivityPub actors should be done
    /// outside of Crowmask, using the admin actor).
    member _.FollowingCollection = dict [
        pair "id" $"{actor}/following"
        pair "type" "Collection"
        pair "totalItems" 0
        pair "items" []
    ]
