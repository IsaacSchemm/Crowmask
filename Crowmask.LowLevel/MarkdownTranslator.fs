namespace Crowmask.LowLevel

open System.Net

/// Creates Markdown and HTML renditions of Crowmask objects and pages, for
/// use in the HTML web interface, or (for debugging) by other non-ActivityPub
/// user agents.
type MarkdownTranslator(mapper: IdMapper, appInfo: ApplicationInformation) =
    /// Performs HTML encoding on a string. (HTML can be inserted into
    /// Markdown and will be included in the final HTML output.)
    let enc = WebUtility.HtmlEncode

    /// Renders an HTML page, given a title and a Markdown document.
    let toHtml (title: string) (str: string) = String.concat "\n" [
        "<!DOCTYPE html>"
        "<html>"
        "<head>"
        "<title>"
        WebUtility.HtmlEncode($"{enc title} - {appInfo.ApplicationName}")
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

    member _.ToMarkdown (person: Person) = String.concat "\n" [
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
        $"[View posts](/api/actor/outbox/page)"
        $"([Atom](/api/actor/outbox/page?format=atom))"
        $"([RSS](/api/actor/outbox/page?format=rss))"
        $""
        $"----------"
        $""
        $"## ActivityPub"
        $""
        $"Follow this account on Mastodon or Pixelfed by copying its handle into your search bar:"
        $""
        for hostname in List.distinct [appInfo.HandleHostname; appInfo.ApplicationHostname] do
            $"    @{enc appInfo.Username}@{hostname}"
        $""
        if not (Seq.isEmpty appInfo.AdminActorIds) then
            $"Any boosts, likes, replies, or mentions will generate a notification to:"
            for adminActorId in appInfo.AdminActorIds do
                $"* [`{enc adminActorId}`]({adminActorId})"
            $""
        $"[View followers](/api/actor/followers)"
        $""
        $"--------"
        $""
        if not (Seq.isEmpty appInfo.AdminActorIds) then
            $"## Bluesky"
            $""
            for account in appInfo.BlueskyBotAccounts do
                $"This server operates a bot account at [`{enc account.DID}`](https://bsky.app/profile/{enc account.DID}) on `{enc account.PDS}`."
                $"Artwork submissions will be mirrored to this account."
                $""
                $"Crowmask does not currently mirror journal entries to Bluesky."
                $""
        $"--------"
        $""
        $"## [{enc appInfo.ApplicationName} {enc appInfo.VersionNumber}]({appInfo.WebsiteUrl}) 🐦‍⬛🎭"
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

    member _.ToMarkdown (gallery: Gallery) = String.concat "\n" [
        $"## Gallery"
        $""
        $"{gallery.gallery_count} item(s)."
        $""
        $"[Start from first page](/api/actor/outbox/page)"
        $""
    ]

    member this.ToHtml (gallery: Gallery) = this.ToMarkdown gallery |> toHtml "Gallery"

    member _.ToMarkdown (page: GalleryPage) = String.concat "\n" [
        $"<style type='text/css'>"
        $"img {{ width: 250px; height: 250px; outline: 1px dashed currentColor; object-fit: scale-down; }}"
        $"</style>"
        $""
        $"## Posts"
        $""
        for post in page.posts do
            let date = post.first_upstream.UtcDateTime.ToString("MMM d, yyyy")
            let post_url = mapper.GetObjectId(post.id)

            $"### [{enc post.title}]({post_url}) ({enc date})"
            $""
            match post.sensitivity, post.id with
            | General, SubmitId _ ->
                for thumbnail in post.thumbnails do
                    $"[![]({thumbnail.url})]({post_url})"
            | General, JournalId _ ->
                $"Cached on {post.first_cached.UtcDateTime.ToLongDateString()}.  "
                $"Click the link to view this journal entry on Weasyl."
            | Sensitive message, _ ->
                enc message
        $""
        if page.posts <> [] then
            $"[View more posts]({mapper.GetNextOutboxPage page})"
        else
            "No more posts are available."
        $""
        $"----------"
        $""
    ]

    member this.ToHtml (page: GalleryPage) = this.ToMarkdown page |> toHtml "Gallery"

    member _.ToMarkdown (followerCollection: FollowerCollection) = String.concat "\n" [
        $"## Followers"
        $""
        for f in followerCollection.followers do
            $"* [{enc f.actorId}]({f.actorId})"
        if List.isEmpty followerCollection.followers then
            $"No followers."
        $""
    ]

    member this.ToHtml (followerCollection: FollowerCollection) = this.ToMarkdown followerCollection |> toHtml "Followers"
