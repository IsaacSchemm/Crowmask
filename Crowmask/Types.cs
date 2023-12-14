using System.Collections.Generic;

namespace Crowmask
{
    public record ActorEndpoint(string sharedInbox = null);

    public record ActorPublicKey(string publicKeyPem = null);

    public record Actor(
        string inbox,
        string outbox = null,
        string followers = null,
        string following = null,
        ActorEndpoint endpoints = null,
        ActorPublicKey publicKey = null);

    public record ActivityPubObject(
        string type = null,
        IEnumerable<string> to = null,
        IEnumerable<string> bto = null,
        IEnumerable<string> cc = null,
        IEnumerable<string> bcc = null);
}
