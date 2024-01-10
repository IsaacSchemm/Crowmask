using Crowmask.DomainModeling;

namespace Crowmask.Library.Cache
{
    public interface IPublicKeyProvider
    {
        Task<IPublicKey> GetPublicKeyAsync();
    }
}
