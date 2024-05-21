namespace Crowmask.LowLevel

open System.Threading.Tasks

/// Provides access to Crowmask's signing key.
type IActorKeyProvider =
    /// Retrieves the public key and renders it in PEM format for use in the ActivityPub actor object.
    abstract member GetPublicKeyAsync: unit -> Task<ActorKey>

    /// Creates an RSA SHA-256 signature for the given data using the private key.
    abstract member SignRsaSha256Async: byte array -> Task<byte array>
