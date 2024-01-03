using System.Security.Cryptography;

namespace Crowmask.Signatures;

public interface ISigningKey
{
    Uri Id { get; }
    RSA GetRsa();
}
