namespace Crowmask.Library.Remote
{
    public record RemoteActor(
        string Id,
        string? Name,
        string Inbox,
        string? SharedInbox,
        string KeyId,
        string KeyPem);
}
