namespace Crowmask.ActivityPub

open System.Collections.Generic
open System.Text.Json

module AP =
    let Context: obj list = [
        "https://w3id.org/security/v1"
        "https://www.w3.org/ns/activitystreams"
        {| sensitive = "as:sensitive" |}
    ]

    let SerializeWithContext (apObject: IDictionary<string, obj>) = JsonSerializer.Serialize(dict [   
        "@context", Context :> obj
        for p in apObject do p.Key, p.Value
    ])
