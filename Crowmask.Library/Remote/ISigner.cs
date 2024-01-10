namespace Crowmask.Library.Remote
{
    public interface ISigner
    {
        Task<byte[]> SignRsaSha256Async(byte[] data);
    }
}
