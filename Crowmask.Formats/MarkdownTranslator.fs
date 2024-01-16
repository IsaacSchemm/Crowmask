namespace Crowmask.Formats

open System.Net
open Crowmask.DomainModeling
open Crowmask.Dependencies.Mapping
open Crowmask.Interfaces

/// Creates Markdown and HTML renditions of Crowmask objects and pages, for
/// use in the HTML web interface, or (for debugging) by other non-ActivityPub\
/// user agents.
type MarkdownTranslator(mapper: ActivityStreamsIdMapper, summarizer: Summarizer, adminActor: IAdminActor, crowmaskHost: ICrowmaskHost, handleHost: IHandleHost, handleName: IHandleName) =
    /// Performs HTML encoding on a string. (HTML can be inserted into
    /// Markdown and will be included in the final HTML output.)
    let enc = WebUtility.HtmlEncode

    /// Renders an HTML page, given a title and a Markdown document.
    let toHtml (title: string) (str: string) = String.concat "\n" [
        "<!DOCTYPE html>"
        "<html>"
        "<head>"
        "<title>"
        WebUtility.HtmlEncode($"{enc title} - Crowmask")
        "</title>"
        "<meta name='viewport' content='width=device-width, initial-scale=1' />"
        "<style type='text/css'>"
        "body { line-height: 1.5; font-family: sans-serif; }"
        "pre { white-space: pre-wrap; }"
        "</style>"
        "</head>"
        "<body>"
        Markdig.Markdown.ToHtml(str)
        "</body>"
        "</html>"
    ]

    /// A Markdown header that is included on all pages.
    let sharedHeader = String.concat "\n" [
        $"This is an ActivityPub mirror of an external artwork gallery. The user at [{enc adminActor.Id}]({adminActor.Id}) will be notified of any likes, shares, or replies."
        $""
        $"[Return to the user profile page]({mapper.ActorId})"
    ]

    member _.ToMarkdown (person: Person) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## {enc person.name}"
        $""
        for iconUrl in person.iconUrls do
            $"![]({iconUrl})"
        $""
        $"{person.summary}"
        $""
        for metadata in person.attachments do
            match metadata.uri with
            | Some uri ->
                $"* {enc metadata.name}: [{enc metadata.value}]({uri})"
            | None ->
                $"* {enc metadata.name}: {enc metadata.value}"
        $""
        $"[View on Weasyl]({person.url})"
        $""
        $"----------"
        $""
        $"## ActivityPub"
        $""
        for hostname in List.distinct [handleHost.Hostname; crowmaskHost.Hostname] do
            $"    @{enc handleName.PreferredUsername}@{hostname}"
        $""
        $"[View outbox](/api/actor/outbox)"
        $""
        $"The outbox contains all submissions posted by {enc person.upstreamUsername}."
        $""
        $"[View followers](/api/actor/followers)"
        $""
        $"--------"
        $""
        $"## Atom/RSS"
        $""
        $"[Atom](/api/actor/outbox/page?format=atom)"
        $""
        $"[RSS](/api/actor/outbox/page?format=rss)"
        $""
        $"--------"
        $""
        $"[Crowmask](https://github.com/IsaacSchemm/Crowmask) 🐦‍⬛🎭"
        $""
        $"Copyright © 2024 Isaac Schemm"
        $""
        $"This program is free software: you can redistribute it and/or modify"
        $"it under the terms of the GNU Affero General Public License as published"
        $"by the Free Software Foundation, either version 3 of the License, or"
        $"(at your option) any later version."
        $""
        $"This program is distributed in the hope that it will be useful,"
        $"but WITHOUT ANY WARRANTY; without even the implied warranty of"
        $"MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the"
        $"GNU Affero General Public License for more details."
    ]

    member this.ToHtml (person: Person) = this.ToMarkdown person |> toHtml person.name

    member _.ToMarkdown (post: Post) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"<style type='text/css'>"
        $"img {{ width: 640px; max-width: 100vw; height: 480px; object-fit: contain }}"
        $"</style>"
        $""
        $"## {enc post.title}"
        $""
        match post.sensitivity with
        | General ->
            for attachment in post.attachments do
                match attachment with Image image ->
                    $"[![]({image.url})]({image.url})"
            $""
            post.content
        | Sensitive message ->
            enc message
        $""
        $"[View on Weasyl]({post.url})"
        $""
        $""
        $"----------"
        $""
        $"**Boosts:** {post.boosts.Length}  "
        $"**Likes:** {post.likes.Length}  "
        $"**Replies:** {post.replies.Length}  "
        $""

        for i in post.Interactions do
            $"* {summarizer.ToMarkdown(post, i)}"

        $""
    ]

    member this.ToHtml (post: Post) = this.ToMarkdown post |> toHtml post.title

    member _.ToMarkdown (post: Post, interaction: Interaction) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"{summarizer.ToMarkdown(post, interaction)}"
        $""
    ]

    member this.ToHtml (post: Post, interaction: Interaction) = this.ToMarkdown (post, interaction) |> toHtml post.title

    member _.ToMarkdown (gallery: Gallery) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## Outbox"
        $""
        $"{gallery.gallery_count} item(s)."
        $""
        $"[Start from first page](/api/actor/outbox/page)"
        $""
    ]

    member this.ToHtml (gallery: Gallery) = this.ToMarkdown gallery |> toHtml "Outbox"

    member _.ToMarkdown (page: Page) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"<style type='text/css'>"
        $"img {{ width: 250px; height: 250px; object-fit: scale-down; }}"
        $"</style>"
        $""
        $"## Posts"
        $""
        for post in page.posts do
            let date = post.first_upstream.UtcDateTime.ToString("MMM d, yyyy")
            let post_url = mapper.GetObjectId(post.submitid)

            $"### [{enc post.title}]({post_url}) ({enc date})"
            $""
            match post.sensitivity with
            | General ->
                for thumbnail in post.thumbnails do
                    $"[![]({thumbnail.url})]({post_url})"
                if post.thumbnails = [] then
                    $"No thumbnail available"
            | Sensitive message ->
                enc message
            $""
        $""
        if page.posts <> [] then
            $"[View more posts](/api/actor/outbox/page?offset={page.offset + page.posts.Length})"
        else
            "No more posts are available."
        $""
        $"----------"
        $""
        $"To interact with a submission via ActivityPub, use the URI format `{mapper.GetObjectId 0}`, where `0` is the numeric ID from the Weasyl submission URI (e.g. `https://www.weasyl.com/~user/submissions/0000000/post-title`)."
        $""
    ]

    member this.ToHtml (page: Page) = this.ToMarkdown page |> toHtml "Posts"

    member _.ToMarkdown (followerCollection: FollowerCollection) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## Followers"
        $""
        for f in followerCollection.followers do
            $"* [{enc f.actorId}]({f.actorId})"
        if List.isEmpty followerCollection.followers then
            $"No followers."
        $""
    ]

    member this.ToHtml (followerCollection: FollowerCollection) = this.ToMarkdown followerCollection |> toHtml "Followers"
