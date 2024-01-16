namespace Crowmask.Formats

open System
open System.Net
open Crowmask.DomainModeling
open Crowmask.Dependencies.Mapping

/// Creates Markdown summaries of interactions with a Crowmask post. These
/// summaries are used in notifications to the admin actor and are shown on
/// the post page in the HTML interface.
type Summarizer(mapper: ActivityStreamsIdMapper) =
    /// Performs HTML encoding on a string. (HTML can be inserted into
    /// Markdown and will be included in the final HTML output.)
    let enc (str: string) =
        WebUtility.HtmlEncode(str)

    /// Renders an HTML-encoded string representation of a date and time.
    let encDate (dt: DateTimeOffset) =
        dt.UtcDateTime.ToString("U") |> enc

    /// Creates a Markdown summary of the given interaction to the given post.
    member _.ToMarkdown(p: Post, e: Interaction) =
        let original_object_id = mapper.GetObjectId(p.submitid)

        match e with
        | Boost i -> $"[`{enc i.actor_id}`]({i.actor_id}) boosted [{enc p.title}]({original_object_id}) ({encDate i.added_at})"
        | Like i -> $"[`{enc i.actor_id}`]({i.actor_id}) liked [{enc p.title}]({original_object_id}) ({encDate i.added_at})"
        | Reply r -> $"[`{enc r.actor_id}`]({r.actor_id}) replied to [{enc p.title}]({original_object_id}) ([{encDate r.added_at}]({r.object_id}))"

    /// Creates a Markdown summary of the given mention.
    member _.ToMarkdown(r: RemotePost) =
        $"[`{enc r.actor_id}`]({r.actor_id}) mentioned [{enc mapper.ActorId}]({mapper.ActorId}) ([{encDate r.added_at}]({r.object_id}))"
