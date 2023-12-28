namespace Crowmask.Markdown

open Microsoft.Net.Http.Headers

type CrowmaskFormat = HTML | ActivityJson | Markdown
with
    static member All = [
        Markdown
        HTML
        ActivityJson
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
    ]

    member this.MediaTypes = [
        for mimeType in this.MimeTypes do
            MediaTypeHeaderValue.Parse($"{mimeType}")
    ]

    member this.MediaType =
        List.head this.MediaTypes

    member this.ContentType =
        $"{this.MediaType}; charset=utf-8"
