namespace Crowmask.LowLevel

open System.Linq
open System.Net.Http.Headers
open Microsoft.Net.Http.Headers
open Crowmask.Interfaces

/// A type of formatting that Crowmask supports for HTTP responses.
type CrowmaskFormatFamily = Markdown | HTML | RedirectActor | RedirectPost | ActivityPub | RSS | Atom

/// An output format that Crowmask supports, consisting of the general type of response (family) and an HTTP Content-Type value.
type CrowmaskFormat = {
    Family: CrowmaskFormatFamily
    MediaType: string
}

/// An object that helps Crowmask determine the appropriate response type for an HTTP request.
type ContentNegotiator(appInfo: IApplicationInformation) =
    /// Builds a CrowmaskFormat object.
    let format family mediaType = {
        Family = family
        MediaType = mediaType
    }

    /// A list of all response types supported by Crowmask, in the order that Crowmask prefers to use them.
    let supported = [
        // Markdown / plain text responses (may be useful for debugging).
        if appInfo.ReturnMarkdown then
            format Markdown "text/plain"
            format Markdown "text/markdown"

        // ActivityPub responses, for intercommunication with other ActivityPub software like Mastodon.
        format ActivityPub "application/activity+json"
        format ActivityPub "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
        format ActivityPub "application/json"
        format ActivityPub "text/json"

        // Redirect web browsers to Weasyl, if enabled.
        if appInfo.RedirectActor then
            format RedirectActor "text/html"
        if appInfo.RedirectPosts then
            format RedirectPost "text/html"

        // HTML responses for web browsers.
        if appInfo.ReturnHTML then
            format HTML "text/html"
    ]

    let parse (str: string) =
        MediaTypeHeaderValue.Parse(str)

    let sortByQuality (values: MediaTypeHeaderValue seq) =
        values.OrderByDescending(id, MediaTypeHeaderValueComparer.QualityComparer)

    /// The RSS 2.0 feed format.
    member _.RSS = format RSS "application/rss+xml"

    // The Atom feed format.
    member _.Atom = format Atom "application/atom+xml"

    /// Given an HTTP request, parses the Accept header to determine which
    /// response type(s) can be used, in order from most to least preferred
    /// (with possible duplicates).
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
