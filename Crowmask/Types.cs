using System;
using System.Text.Json.Serialization;

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

    public record APObject(
        string id = null,
        string type = null,
        string attributedTo = null,
        string content = null,
        DateTimeOffset? published = null,
        string[] to = null,
        string[] cc = null);

    public record Activity(
        string type = null,
        string id = null,
        string actor = null,
        DateTimeOffset? published = null,
        string[] to = null,
        string[] cc = null,
        APObject @object = null)
    {
        [JsonPropertyName("@context")]
        public string Context => "https://www.w3.org/ns/activitystreams";
    }
}
