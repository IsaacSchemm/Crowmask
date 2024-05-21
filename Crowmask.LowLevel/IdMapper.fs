namespace Crowmask.LowLevel

open System
open Crowmask.Data

/// Provides mappings between Crowmask's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: ApplicationInformation) =
    /// The ActivityPub actor ID of the single actor hosted by this Crowmask instance.
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}/api/actor"

    /// Generates a random ID that is not intended to be looked up.
    /// Used for Update and Delete activities.
    member _.GenerateTransientId() =
        $"https://{appInfo.ApplicationHostname}#transient-{Guid.NewGuid()}"

    /// Determines the ActivityPub object ID for a post.
    member _.GetObjectId(id: PostId) =
        match id with
        | SubmitId submitid -> $"https://{appInfo.ApplicationHostname}/api/submissions/{submitid}"
        | JournalId journalid -> $"https://{appInfo.ApplicationHostname}/api/journals/{journalid}"

    /// Determines the ID to use for a Create activity for a post.
    member this.GetCreateId(id: PostId) =
        $"{this.GetObjectId(id)}#create"

    /// Determines the URL for a notification post sent to the admin actor.
    member _.GetObjectId(interaction: Interaction) =
        $"https://{appInfo.ApplicationHostname}/api/interactions/{interaction.Id}/notification"

    /// Determines the URL for a notification post sent to the admin actor.
    member _.GetObjectId(remoteMention: Mention) =
        $"https://{appInfo.ApplicationHostname}/api/mentions/{remoteMention.Id}/notification"

    /// Determines the URL for the next page of the outbox.
    member _.GetNextOutboxPage(ids: PostId seq) =
        match Seq.min ids with
        | SubmitId submitid -> $"/api/actor/outbox/page?nextid={submitid}"
        | JournalId journalid -> $"/api/actor/outbox/page?nextid={journalid}&type=journal"
