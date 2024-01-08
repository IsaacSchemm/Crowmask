namespace Crowmask.DomainModeling

open System

type ActivityStreamsIdMapper(host: ICrowmaskHost) =
    member _.ActorId =
        $"https://{host.Hostname}/api/actor"

    member _.GetTransientId() =
        $"https://{host.Hostname}/api/actor#transient-{Guid.NewGuid()}"

    member _.GetObjectType(identifier: JointIdentifier) =
        match identifier with
        | SubmissionIdentifier _ -> "Note"
        | JournalIdentifier _ -> "Article"

    member _.GetObjectId(identifier: JointIdentifier) =
        match identifier with
        | SubmissionIdentifier submitid -> $"https://{host.Hostname}/api/submissions/{submitid}"
        | JournalIdentifier journalid -> $"https://{host.Hostname}/api/journals/{journalid}"

    member this.GetObjectId(identifier: JointIdentifier, interaction: Interaction) =
        $"{this.GetObjectId(identifier)}/interactions/{interaction.Id}/notification"
