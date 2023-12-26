using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Crowmask.ActivityPub;
using System;
using System.Threading.Tasks;

namespace Crowmask
{
    public class KeyProvider : IKeyProvider
    {
        private record PublicKey(string Pem) : IPublicKey;

        private readonly KeyClient _keyClient;

        public KeyProvider(Uri vaultUri)
        {
            var tokenCredential = new DefaultAzureCredential();
            _keyClient = new KeyClient(vaultUri, tokenCredential);
        }

        public async Task<IPublicKey> GetPublicKeyAsync()
        {
            var key = await _keyClient.GetKeyAsync("crowmask-ap");
            byte[] arr = key.Value.Key.ToRSA().ExportSubjectPublicKeyInfo();
            string str = Convert.ToBase64String(arr);
            return new PublicKey($"-----BEGIN PUBLIC KEY-----\n{str}\n-----END PUBLIC KEY-----");
        }

        public async Task<byte[]> SignRsaSha256Async(byte[] data)
        {
            var cryptographyClient = _keyClient.GetCryptographyClient("crowmask-ap");
            var result = await cryptographyClient.SignDataAsync(SignatureAlgorithm.RS256, data);
            return result.Signature;
        }
    }
}
