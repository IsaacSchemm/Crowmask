namespace Crowmask.Markdown

open System.Linq
open System.Net.Http.Headers
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

    let Sort (headers: MediaTypeHeaderValue seq) =
        headers.OrderByDescending(id, MediaTypeHeaderValueComparer.QualityComparer)

    let ForValue (accept: MediaTypeHeaderValue) = seq {
        for format in [Markdown; HTML; ActivityJson] do
            for correspondingType in format.MediaTypes do
                if correspondingType.IsSubsetOf(accept) then
                    yield format
    }

    let ForHeaders (headers: HttpHeaders) =
        headers.GetValues("Accept")
        |> Seq.map (fun str -> MediaTypeHeaderValue.Parse(str))
        |> Sort
        |> Seq.collect ForValue
