namespace Crowmask.Markdown

open Microsoft.Net.Http.Headers

type CrowmaskFormat = HTML | ActivityJson | Markdown | RSS | Atom
with
    static member All = [
        Markdown
        HTML
        ActivityJson
        RSS
        Atom
    ]

    member this.MimeTypes = [
        match this with
        | HTML ->
            "text/html"
            "application/xhtml+xml"
        | Markdown ->
            "text/markdown"
            "text/plain"
        | ActivityJson ->
            "application/activity+json"
            "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
            "application/json"
            "text/json"
        | RSS ->
            "application/rss+xml"
            "application/xml"
            "text/xml"
        | Atom ->
            "application/atom+xml"
    ]

    member this.MediaTypes = [
        for mimeType in this.MimeTypes do
            MediaTypeHeaderValue.Parse($"{mimeType}")
    ]

    member this.MediaType =
        List.head this.MediaTypes
