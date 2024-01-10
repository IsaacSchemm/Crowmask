using System.Security.Cryptography;

namespace Crowmask.Library.Signatures;

public interface ISigningKey
{
    Uri Id { get; }
    RSA GetRsa();
}
