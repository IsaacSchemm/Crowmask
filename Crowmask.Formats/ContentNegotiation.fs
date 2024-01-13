namespace Crowmask.Formats

open System.Linq
open System.Net.Http.Headers
open Microsoft.Net.Http.Headers
open Crowmask.Interfaces

type CrowmaskFormatFamily = Markdown | HTML | UpstreamRedirect | ActivityPub | RSS | Atom

type CrowmaskFormat = {
    Family: CrowmaskFormatFamily
    MediaType: string
}

type ContentNegotiator(configuration: IContentNegotiationConfiguration) =
    let format family mediaType = {
        Family = family
        MediaType = mediaType
    }

    let supported = [
        if configuration.ReturnMarkdown then
            format Markdown "text/plain"
            format Markdown "text/markdown"

        format ActivityPub "application/activity+json"
        format ActivityPub "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
        format ActivityPub "application/json"
        format ActivityPub "text/json"

        if configuration.UpstreamRedirect then
            format UpstreamRedirect "text/html"

        if configuration.ReturnHTML then
            format HTML "text/html"
    ]

    let parse (str: string) =
        MediaTypeHeaderValue.Parse(str)

    let sortByQuality (values: MediaTypeHeaderValue seq) =
        values.OrderByDescending(id, MediaTypeHeaderValueComparer.QualityComparer)

    member _.RSS = format RSS "application/rss+xml"
    member _.Atom = format Atom "application/atom+xml"

    member _.GetAcceptableFormats(headers: HttpHeaders) = seq {
        let parsed =
            headers.GetValues("Accept")
            |> Seq.map parse
            |> sortByQuality
        for acceptedType in parsed do
            for candidate in supported do
                let responseType = parse candidate.MediaType
                if responseType.IsSubsetOf(acceptedType) then
                    yield candidate
    }
