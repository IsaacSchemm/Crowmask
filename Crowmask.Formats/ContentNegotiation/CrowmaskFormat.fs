﻿namespace Crowmask.Formats.ContentNegotiation

open Microsoft.Net.Http.Headers

/// A response type supported by Crowmask's HTTP content negotiation.
type CrowmaskFormat = HTML | ActivityStreams | Markdown | RSS | Atom
with
    /// All response types that can be supported by Crowmask's HTTP content
    /// negotiation, in order of Crowmask's preference when the user agent does
    /// not express its own.
    ///
    /// The default output format of Crowmask is the Markdown that it uses to
    /// generate its HTML. ActivityPub applications and web browers should use
    /// appropriate Accept headers to request ActivityStreams JSON or HTML,
    /// respectively.
    ///
    /// RSS and Atom feeds can be requested through content negotiation or
    /// through a query parameter on the OutboxPage endpoint.
    static member All = [
        Markdown
        HTML
        ActivityStreams
        RSS
        Atom
    ]

    /// Media types that correspond to this format.
    member this.MediaTypes = [
        for mimeType in [
            match this with
            | HTML ->
                "text/html"
                "application/xhtml+xml"
            | Markdown ->
                "text/markdown"
                "text/plain"
            | ActivityStreams ->
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
        ] do
            MediaTypeHeaderValue.Parse($"{mimeType}")
    ]

    /// The media type to use as the Content-Type of the HTTP response.
    member this.MediaType =
        List.head this.MediaTypes