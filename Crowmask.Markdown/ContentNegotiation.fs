namespace Crowmask.Markdown

open System.Linq
open Microsoft.Net.Http.Headers

module ContentNegotiation =
    type CrowmaskFormat = HTML | ActivityJson | Markdown
    with
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

    let FindAppropriateFormats (headers: MediaTypeHeaderValue seq) = [
        let ordered = headers.OrderByDescending(id, MediaTypeHeaderValueComparer.QualityComparer)
        for acceptedType in ordered do
            for format in [Markdown; ActivityJson; HTML] do
                for correspondingType in format.MediaTypes do
                    if correspondingType.IsSubsetOf(acceptedType) then
                        yield format
    ]
