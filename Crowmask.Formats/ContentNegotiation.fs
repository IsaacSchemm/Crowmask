namespace Crowmask.Formats

open System.Net.Http.Headers
open Microsoft.Net.Http.Headers

type CrowmaskFormatFamily = Markdown | HTML | ActivityPub | RSS | Atom

type CrowmaskFormat = {
    Family: CrowmaskFormatFamily
    MediaType: string
}

module ContentNegotiation =
    let private format family mediaType = {
        Family = family
        MediaType = mediaType
    }

    let Supported = [
        format Markdown "text/plain"
        format Markdown "text/markdown"
        format HTML "text/html"
        format ActivityPub "application/activity+json"
        format ActivityPub "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
        format ActivityPub "application/json"
        format ActivityPub "text/json"
        format RSS "application/rss+xml"
        format RSS "application/xml"
        format RSS "text/xml"
        format Atom "application/atom+xml"
        format Atom "application/xml"
        format Atom "text/xml"
    ]

    let RSS = format RSS "application/rss+xml"
    let Atom = format Atom "application/atom+xml"

type ContentNegotiator() =
    let parse (str: string) =
        MediaTypeHeaderValue.Parse(str)

    let sortByQuality (values: MediaTypeHeaderValue seq) =
        System.Linq.Enumerable.OrderByDescending(
            values,
            id,
            MediaTypeHeaderValueComparer.QualityComparer)

    member _.GetAcceptableFormats(headers: HttpHeaders) = seq {
        let parsed =
            headers.GetValues("Accept")
            |> Seq.map parse
            |> sortByQuality
        for acceptedType in parsed do
            for candidate in ContentNegotiation.Supported do
                let responseType = parse candidate.MediaType
                if responseType.IsSubsetOf(acceptedType) then
                    yield candidate
    }
