namespace Crowmask.Remote
{
    public interface ISigner
    {
        Task<byte[]> SignRsaSha256Async(byte[] data);
    }
}
