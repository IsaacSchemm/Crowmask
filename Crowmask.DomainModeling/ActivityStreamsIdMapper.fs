namespace Crowmask.DomainModeling

open System

type ActivityStreamsIdMapper(host: ICrowmaskHost) =
    member _.ActorId =
        $"https://{host.Hostname}/api/actor"

    member _.GetTransientId() =
        $"https://{host.Hostname}#transient-{Guid.NewGuid()}"

    member _.GetObjectType(upstream_type) =
        match upstream_type with
        | UpstreamSubmission _ -> "Note"
        | UpstreamJournal _ -> "Article"

    member _.GetObjectId(upstream_type) =
        match upstream_type with
        | UpstreamSubmission submitid -> $"https://{host.Hostname}/api/submissions/{submitid}"
        | UpstreamJournal journalid -> $"https://{host.Hostname}/api/journals/{journalid}"

    member this.GetObjectId(upstream_type: UpstreamType, interaction: Interaction) =
        $"{this.GetObjectId(upstream_type)}/interactions/{interaction.Id}/notification"
