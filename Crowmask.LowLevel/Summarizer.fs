namespace Crowmask.LowLevel

open System
open System.Net
open Crowmask.Data

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
    member _.ToMarkdown(i: Interaction) =
        $"[`{enc i.ActorId}`]({i.ActorId}) performed {enc i.ActivityType} on [a post]({i.TargetId}) ({encDate i.AddedAt})"

    /// Creates a Markdown summary of the given mention.
    member _.ToMarkdown(r: Mention) =
        $"New mention/reply at ([{encDate r.AddedAt}]({r.ObjectId})) from [`{enc r.ActorId}`]({r.ActorId})"
