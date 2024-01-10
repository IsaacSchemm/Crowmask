namespace Crowmask.InteractionSummaries

open System
open System.Net
open Crowmask.DomainModeling
open Crowmask.Interfaces

type InteractionSummarizer(mapper: IActivityStreamsIdMapper) =
    let enc (str: string) =
        WebUtility.HtmlEncode(str)

    let encDate (dt: DateTimeOffset) =
        dt.UtcDateTime.ToString("U") |> enc

    interface IInteractionSummarizer with
        member _.ToMarkdown (p: Post, e: Interaction) =
            let original_object_id = mapper.GetObjectId p.identifier

            match e with
            | Boost i -> $"[`{enc i.actor_id}`]({i.actor_id}) boosted [{enc p.title}]({original_object_id}) ({encDate i.added_at})"
            | Like i -> $"[`{enc i.actor_id}`]({i.actor_id}) liked [{enc p.title}]({original_object_id}) ({encDate i.added_at})"
            | Reply r -> $"[`{enc r.actor_id}`]({r.actor_id}) replied to [{enc p.title}]({original_object_id}) ([{encDate r.added_at}]({r.object_id}))"

        member this.ToHtml (p: Post, e: Interaction) =
            (this :> IInteractionSummarizer).ToMarkdown (p, e) |> Markdig.Markdown.ToHtml
