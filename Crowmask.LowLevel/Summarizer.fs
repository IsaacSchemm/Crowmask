namespace Crowmask.LowLevel

open System
open System.Net

/// Creates Markdown summaries of interactions with a Crowmask post. These
/// summaries are used in notifications to the admin actor and are shown on
/// the post page in the HTML interface.
type Summarizer() =
    /// Performs HTML encoding on a string. (HTML can be inserted into
    /// Markdown and will be included in the final HTML output.)
    let enc (str: string) =
        WebUtility.HtmlEncode(str)

    /// Renders an HTML-encoded string representation of a date and time.
    let encDate (dt: DateTimeOffset) =
        dt.UtcDateTime.ToString("U") |> enc

    /// Creates a Markdown summary of the given interaction to the given post.
    member _.ToMarkdown(i: RemoteInteraction) =
        $"[`{enc i.actor_id}`]({i.actor_id}) performed {enc i.activity_type} on [a post]({i.target_id}) ({encDate i.added_at})"

    /// Creates a Markdown summary of the given mention.
    member _.ToMarkdown(r: RemoteMention) =
        $"New mention/reply at ([{encDate r.added_at}]({r.object_id})) from [`{enc r.actor_id}`]({r.actor_id})"
