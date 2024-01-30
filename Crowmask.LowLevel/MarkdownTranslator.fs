namespace Crowmask.LowLevel

open System.Net
open Crowmask.Interfaces
open Crowmask.Data

/// Creates Markdown and HTML renditions of Crowmask objects and pages, for
/// use in the HTML web interface, or (for debugging) by other non-ActivityPub
/// user agents.
type MarkdownTranslator(mapper: IdMapper, summarizer: Summarizer, appInfo: IApplicationInformation) =
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

    /// A Markdown header that is included on all pages.
    let sharedHeader = String.concat "\n" [
        $"# [@{enc appInfo.Username}]({mapper.ActorId})"
        $""
        $"ActivityPub mirror powered by [{enc appInfo.ApplicationName}]({appInfo.WebsiteUrl})"
    ]

    member _.ToMarkdown (person: Person, recentSubmissions: Post seq) = String.concat "\n" [
        sharedHeader
        $""
        $"<style type='text/css'>"
        $"table img {{ width: 250px; height: 250px; object-fit: scale-down; }}"
        $"</style>"
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
        $"----------"
        $""
        $"## Gallery"
        $""
        $"<table><tr>"
        for post in recentSubmissions do
            for thumbnail in post.thumbnails do
                $"<td><a href='{mapper.GetObjectId(post.submitid)}'>"
                $"<img src='{thumbnail.url}' />"
                $""
                $"**{enc post.title}**  "
                $"{enc (post.first_upstream.UtcDateTime.ToLongDateString())}"
                $""
                $"</a></td>"
        $"</tr></table>"
        $""
        $"[View gallery](/api/actor/outbox/page)"
        $""
        $"----------"
        $""
        $"## ActivityPub"
        $""
        for hostname in List.distinct [appInfo.HandleHostname; appInfo.ApplicationHostname] do
            $"    @{enc appInfo.Username}@{hostname}"
        $""
        $"Any boosts, likes, replies, or mentions will generate a notification to [{enc appInfo.AdminActorId}]({appInfo.AdminActorId})."
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

    member this.ToHtml (person: Person, recentSubmissions: Post seq) = this.ToMarkdown (person, recentSubmissions) |> toHtml person.name

    member _.ToMarkdown (post: Post) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"<style type='text/css'>"
        $"img {{ width: 640px; max-width: 100vw; height: 480px; object-fit: contain }}"
        $"</style>"
        $""
        match post.sensitivity with
        | General ->
            for image in post.images do
                $"[![]({image.url})]({image.url})"
            $""
            post.content
        | Sensitive message ->
            enc message
        $""
        $"[View on Weasyl]({post.url})"
        $""
    ]

    member this.ToHtml (post: Post) = this.ToMarkdown post |> toHtml post.title

    member _.ToMarkdown (interaction: Interaction) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"{summarizer.ToMarkdown(interaction)}"
        $""
    ]

    member this.ToHtml (interaction: Interaction) = this.ToMarkdown (interaction) |> toHtml "Crowmask"

    member _.ToMarkdown (mention: Mention) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"{summarizer.ToMarkdown(mention)}"
        $""
    ]

    member this.ToHtml (mention: Mention) = this.ToMarkdown (mention) |> toHtml "Crowmask"

    member _.ToMarkdown (gallery: Gallery) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## Gallery"
        $""
        $"{gallery.gallery_count} item(s)."
        $""
        $"[Start from first page](/api/actor/outbox/page)"
        $""
    ]

    member this.ToHtml (gallery: Gallery) = this.ToMarkdown gallery |> toHtml "Gallery"

    member _.ToMarkdown (page: GalleryPage) = String.concat "\n" [
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
            $"[View more posts](/api/actor/outbox/page?nextid={List.min [for p in page.posts do p.submitid]})"
        else
            "No more posts are available."
        $""
        $"----------"
        $""
        $"To interact with a submission via ActivityPub, use the URI format `{mapper.GetObjectId 0}`, where `0` is the numeric ID from the Weasyl submission URI (e.g. `https://www.weasyl.com/~user/submissions/0000000/post-title`)."
        $""
    ]

    member this.ToHtml (page: GalleryPage) = this.ToMarkdown page |> toHtml "Gallery"

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
