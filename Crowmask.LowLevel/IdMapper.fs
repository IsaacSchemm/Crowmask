namespace Crowmask.LowLevel

open System
open Crowmask.Interfaces

/// Provides mappings between Crowmask's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: IApplicationInformation) =
    /// The ActivityPub actor ID of the single actor hosted by this Crowmask instance.
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}/api/actor"

    /// Generates a random ID that is not intended to be looked up.
    /// Used for Update and Delete activities.
    member _.GenerateTransientId() =
        $"https://{appInfo.ApplicationHostname}#transient-{Guid.NewGuid()}"

    /// Determines the ActivityPub object ID for a post.
    member _.GetObjectId(submitid: int) =
        $"https://{appInfo.ApplicationHostname}/api/submissions/{submitid}"

    /// Determines the ID to use for a Create activity for a post.
    member this.GetCreateId(submitid: int) =
        $"{this.GetObjectId(submitid)}#create"

    /// Determines the URL for a notification post sent to the admin actor.
    member _.GetObjectId(interaction: RemoteInteraction) =
        $"https://{appInfo.ApplicationHostname}/api/interactions/{interaction.id}/notification"

    /// Determines the URL for a notification post sent to the admin actor.
    member _.GetObjectId(remoteMention: RemoteMention) =
        $"https://{appInfo.ApplicationHostname}/api/mentions/{remoteMention.id}/notification"
