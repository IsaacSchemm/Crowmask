using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Crowmask.Interfaces;
using System;
using System.Threading.Tasks;

namespace Crowmask
{
    /// <summary>
    /// Provides access to an encryption key in Azure Key Vault. This key is
    /// used as the signing key for the ActivityPub actor.
    /// </summary>
    public class KeyProvider : ICrowmaskKeyProvider
    {
        private record PublicKey(string Pem) : ICrowmaskKey;

        private readonly KeyClient _keyClient;

        /// <summary>
        /// Creates a new KeyProvider and initializes an Azure Key Vault client.
        /// </summary>
        /// <param name="vaultUri">The URI of the Azure Key Vault instance</param>
        public KeyProvider(IKeyVaultHost host)
        {
            var vaultUri = new Uri($"https://{host.Hostname}");
            var tokenCredential = new DefaultAzureCredential();
            _keyClient = new KeyClient(vaultUri, tokenCredential);
        }

        /// <summary>
        /// Retrieves the public key and renders it in PEM format for use in the ActivityPub actor object.
        /// </summary>
        /// <returns>An object that contains the public key in PEM format</returns>
        public async Task<ICrowmaskKey> GetPublicKeyAsync()
        {
            var key = await _keyClient.GetKeyAsync("crowmask-ap");
            byte[] arr = key.Value.Key.ToRSA().ExportSubjectPublicKeyInfo();
            string str = Convert.ToBase64String(arr);
            return new PublicKey($"-----BEGIN PUBLIC KEY-----\n{str}\n-----END PUBLIC KEY-----");
        }

        /// <summary>
        /// Creates a signature for the given data usign the private key.
        /// </summary>
        /// <param name="data">The data to sign</param>
        /// <returns>An RSA SHA-256 signature</returns>
        public async Task<byte[]> SignRsaSha256Async(byte[] data)
        {
            var cryptographyClient = _keyClient.GetCryptographyClient("crowmask-ap");
            var result = await cryptographyClient.SignDataAsync(SignatureAlgorithm.RS256, data);
            return result.Signature;
        }
    }
}
