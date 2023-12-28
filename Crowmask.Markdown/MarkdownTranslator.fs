namespace Crowmask.Markdown

open System.Collections.Generic
open System.Net
open Crowmask.DomainModeling

type MarkdownTranslator(adminActor: IAdminActor, crowmaskHost: ICrowmaskHost, handleHost: IHandleHost) =
    let toHtml (title: string) (str: string) = String.concat "\n" [
        "<!DOCTYPE html>"
        "<html>"
        "<head>"
        "<title>"
        WebUtility.HtmlEncode($"{title} - Crowmask")
        "</title>"
        "<meta name='viewport' content='width=device-width, initial-scale=1' />"
        "<style type='text/css'>"
        "body { line-height: 1.5; font-family: sans-serif; }"
        "img { max-width: 300px; max-height: 240px; }"
        "pre { white-space: pre-wrap; }"
        "</style>"
        "</head>"
        "<body>"
        Markdig.Markdown.ToHtml(str)
        "</body>"
        "</html>"
    ]

    let sharedHeader = String.concat "\n" [
        $"# Crowmask"
        $""
        $"This is an ActivityPub mirror powered by [Crowmask](https://github.com/IsaacSchemm/Crowmask). The ActivityPub user at [{adminActor.Id}]({adminActor.Id}) will be notified of any likes, shares, or replies."
        $""
        $"[Return to the user profile page](/api/actor)"
    ]

    member _.ToMarkdown (person: Person) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## {person.name}"
        $""
        for iconUrl in person.iconUrls do
            $"![]({iconUrl})"
        $""
        $"{person.summary}"
        $""
        for metadata in person.attachments do
            match metadata.uri with
            | Some uri ->
                $"* {metadata.name}: [{metadata.value}]({uri.AbsoluteUri})"
            | None ->
                $"* {metadata.name}: {metadata.value}"
        $""
        $"[View original profile]({person.url})"
        $""
        $"----------"
        $""
        $"## ActivityPub"
        $""
        for hostname in List.distinct [handleHost.Hostname; crowmaskHost.Hostname] do
            $"    @{person.preferredUsername}@{hostname}"
        $""
        $"[View followers](/api/actor/followers)"
        $""
        $"[Browse posts](/api/actor/outbox)"
        $""
        $"🐦‍⬛🎭"
    ]

    member this.ToHtml (person: Person) = this.ToMarkdown person |> toHtml person.name

    member _.OutboxMarkdown = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## Outbox"
        $""
        $"[Start from first page](/api/actor/outbox/page)"
        $""
        $"[Start from last page](/api/actor/outbox/page?backid=1)"
        $""
    ]

    member this.OutboxHtml = toHtml "Outbox" this.OutboxMarkdown

    member _.ToMarkdown (outbox: IReadOnlyList<Post>) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## Outbox"
        $""
        for post in outbox do
            let date = post.first_upstream.UtcDateTime.ToString("MMM d, yyyy")
            $"* [{post.title}](/api/submissions/{post.submitid}) ({date})"
        $""
        match Domain.GetExtrema outbox with
        | Some extrema ->
            $"[View newer posts](/api/actor/outbox/page?backid={extrema.backid}) ·[View older posts](/api/actor/outbox/page?nextid={extrema.nextid})"
        | None ->
            $"[Restart from first page](/api/actor/outbox/page) · [Restart from last page](/api/actor/outbox/page?backid=1)"
        $""
        for post in outbox |> Seq.truncate 1 do
            $"----------"
            $""
            $"To interact with a specific post, add the numeric ID from the Weasyl submission URL to the `/api/submissions/` endpoint. For example, the submission at:"
            $""
            $"    {post.url}"
            $""
            $"can be accessed at:"
            $""
            $"    https://{crowmaskHost.Hostname}/api/submissions/{post.submitid}"
            $""
    ]

    member this.ToHtml (outbox: IReadOnlyList<Post>) = this.ToMarkdown outbox |> toHtml "Outbox"

    member _.ToMarkdown (post: Post) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## {post.title}"
        $""
        if post.sensitivity = Sensitivity.General then
            for attachment in post.attachments do
                match attachment with Image image ->
                    $"![]({image.url})"
            $""
            post.content
        $""
        $"[View on Weasyl]({post.url})"
    ]

    member this.ToHtml (post: Post) = this.ToMarkdown post |> toHtml post.title

    member _.FollowersMarkdown = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## Followers"
        $""
        $"[Start from first page](/api/actor/followers/page)"
        $""
    ]

    member this.FollowersHtml = toHtml "Followers" this.FollowersMarkdown

    member _.ToMarkdown (followers: IReadOnlyList<Crowmask.Data.Follower>) = String.concat "\n" [
        sharedHeader
        $""
        $"--------"
        $""
        $"## Followers"
        $""
        for f in followers do
            $"* [{f.ActorId}]({f.ActorId})"
        $""
        let ids = [for f in followers do f.Id]
        if ids <> [] then
            $"[View more](/api/actor/followers/page?after={Seq.max ids})"
    ]

    member this.ToHtml (outbox: IReadOnlyList<Crowmask.Data.Follower>) = this.ToMarkdown outbox |> toHtml "Outbox"

