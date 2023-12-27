using Crowmask.ActivityPub;

namespace Crowmask.Cache
{
    public interface IPublicKeyProvider
    {
        Task<IPublicKey> GetPublicKeyAsync();
    }
}
