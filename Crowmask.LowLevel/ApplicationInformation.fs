namespace Crowmask.LowLevel

type ApplicationInformation = {
    /// The application name (e.g. "Crowmask").
    ApplicationName: string

    /// The Crowmask version number.
    VersionNumber: string

    /// The host / domain name used by Crowmask.
    /// May or may not be the same as the domain in the actor's handle.
    ApplicationHostname: string

    /// A URL to a website with more information about the application.
    WebsiteUrl: string

    /// The username of the Crowmask actor (used in the @ handle).
    Username: string

    /// The host / domain name used in the Crowmask actor's preferred handle.
    /// May or may not be the same as Crowmask's domain.
    HandleHostname: string

    /// The ActivityPub IDs of the admin actors, ActivityPub users who should
    /// be notified when Crowmask recieves a boost, like, reply, or mention.
    AdminActorIds: string list

    /// A list of Bluesky accounts that Crowmask should create and delete
    /// posts on (mirroring its ActivityPub posts).
    BlueskyBotAccounts: BlueskyAccountConfiguration list
} with
    /// The user agent string for outgoing requests.
    member this.UserAgent = $"{this.ApplicationName}/{this.VersionNumber} ({this.WebsiteUrl})"
