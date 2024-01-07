namespace Crowmask.Markdown

open System
open System.Net
open Crowmask.DomainModeling

type EngagementTranslator(mapper: ActivityStreamsIdMapper) =
    let enc (str: string) =
        WebUtility.HtmlEncode(str)

    let encDate (dt: DateTimeOffset) =
        dt.UtcDateTime.ToString("U") |> enc

    member _.ToMarkdown (p: Post) (e: Engagement) =
        let original_object_id = mapper.GetObjectId p.upstream_type

        match e with
        | Boost i ->
            $"[`{enc i.actor_id}`]({i.actor_id}) boosted [{enc p.title}]({original_object_id}): {encDate i.added_at}"
        | Like i ->
            $"[`{enc i.actor_id}`]({i.actor_id}) liked [{enc p.title}]({original_object_id}): {encDate i.added_at}"
        | Reply r ->
            $"[`{enc r.actor_id}`]({r.actor_id}) replied to [{enc p.title}]({original_object_id}): [{encDate r.added_at}]({r.object_id})"

    member this.ToHtml (p: Post) (e: Engagement) = this.ToMarkdown p e |> Markdig.Markdown.ToHtml
