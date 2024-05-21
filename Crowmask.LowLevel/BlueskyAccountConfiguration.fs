namespace Crowmask.LowLevel

/// An account on Bluesky or a compatible atproto app, with optional username  and password credentials.
type BlueskyAccountConfiguration = {
    /// The PDS to connect to (e.g. bsky.social).
    PDS: string

    /// The user's DID.
    DID: string

    /// The login username, if available.
    Identifier: string

    /// The login password, if available.
    Password: string
}
