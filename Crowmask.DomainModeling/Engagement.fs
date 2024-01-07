namespace Crowmask.DomainModeling

open System
open System.Net

type Engagement = Boost of Interaction | Like of Interaction | Reply of Reply

module Engagement =
    let private enc (str: string) =
        WebUtility.HtmlEncode(str)

    let private encDate (dt: DateTimeOffset) =
        dt.UtcDateTime.ToString("U") |> enc

    let getDate e =
        match e with
        | Boost i -> i.added_at
        | Like i -> i.added_at
        | Reply r -> r.added_at

    let getAll (post: Post) =
        seq {
            for i in post.boosts do Boost i
            for i in post.likes do Like i
            for r in post.replies do Reply r
        }
        |> Seq.sortBy getDate
        |> Seq.toList

    let getMarkdown e =
        match e with
        | Boost i -> $"[`{enc i.actor_id}`]({i.actor_id}) boosted: {encDate i.added_at}"
        | Like i -> $"[`{enc i.actor_id}`]({i.actor_id}) liked: {encDate i.added_at}"
        | Reply r -> $"[`{enc r.actor_id}`]({r.actor_id}) replied: [{encDate r.added_at}]({r.object_id})"
