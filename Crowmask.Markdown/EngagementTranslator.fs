namespace Crowmask.Markdown

open System
open System.Net
open Crowmask.DomainModeling

type EngagementTranslator(mapper: ActivityStreamsIdMapper) =
    let enc (str: string) =
        WebUtility.HtmlEncode(str)

    let encDate (dt: DateTimeOffset) =
        dt.UtcDateTime.ToString("U") |> enc

    member _.ToMarkdown (pe: PostEngagement) =
        let original_object_id = mapper.GetObjectId pe.post.upstream_type

        match pe.engagement with
        | Boost i ->
            $"[`{enc i.actor_id}`]({i.actor_id}) boosted [{enc pe.post.title}]({original_object_id}): {encDate i.added_at}"
        | Like i ->
            $"[`{enc i.actor_id}`]({i.actor_id}) liked [{enc pe.post.title}]({original_object_id}): {encDate i.added_at}"
        | Reply r ->
            $"[`{enc r.actor_id}`]({r.actor_id}) replied to [{enc pe.post.title}]({original_object_id}): [{encDate r.added_at}]({r.object_id})"

    member this.ToHtml (pe: PostEngagement) = this.ToMarkdown pe |> Markdig.Markdown.ToHtml
