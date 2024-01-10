using Crowmask.Signatures;
using System.Security.Cryptography;

namespace Crowmask.Remote
{
    public record RemoteActor(
            string Id,
            string? Name,
            string Inbox,
            string? SharedInbox,
            string KeyId,
            string KeyPem) : ISigningKey
    {
        Uri ISigningKey.Id => new(KeyId);
        RSA ISigningKey.GetRsa()
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(KeyPem);
            return rsa;
        }
    }
}
