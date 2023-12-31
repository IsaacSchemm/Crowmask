namespace Crowmask.DomainModeling

type CacheResult = Updated of Post |  Found of Post | Deleted | NotFound
with
    member this.Changed =
        match this with
        | Updated _ | Deleted -> true
        | Found _ | NotFound -> false
    member this.AsList =
        match this with
        | Updated p
        | Found p -> [p]
        | Deleted
        | NotFound -> []
