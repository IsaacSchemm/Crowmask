namespace Crowmask.Formats.ActivityPub

open System.Collections.Generic
open System.Text.Json

/// Contains functions for JSON-LD serialization.
module AP =
    /// A JSON-LD context that includes all fields used by Crowmask.
    let Context: obj list = [
        "https://w3id.org/security/v1"
        "https://www.w3.org/ns/activitystreams"
        {| 
            // https://docs.joinmastodon.org/spec/activitypub/#as
            Hashtag = "as:Hashtag"
            sensitive = "as:sensitive"
            // https://docs.joinpeertube.org/api/activitypub#example-2
            comments = "as:comments"
        |}
    ]

    /// Converts ActivityPub objects in string/object pair format to an
    /// acceptable JSON-LD rendition.
    let SerializeWithContext (apObject: IDictionary<string, obj>) = JsonSerializer.Serialize(dict [   
        "@context", Context :> obj
        for p in apObject do p.Key, p.Value
    ])
