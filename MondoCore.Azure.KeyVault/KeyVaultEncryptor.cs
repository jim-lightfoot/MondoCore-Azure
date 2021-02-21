using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;

using MondoCore.Common;
using MondoCore.Security.Encryption;


namespace MondoCore.Azure.KeyVault
{
    public class KeyVaultEncryptor : IEncryptor
    {
        private readonly CryptographyClient  _client;
        private readonly EncryptionAlgorithm _algorithm;
        private static readonly EncryptionPolicy _policy = new EncryptionPolicy { IsReadOnly = true };

        public EncryptionPolicy Policy => _policy;

        public KeyVaultEncryptor(Uri uri, string tenantId, string clientId, string secret, Algorithm algorithm = Algorithm.Rsa15)
                          : this(uri, new ClientSecretCredential(tenantId, clientId, secret), algorithm)
        {
        }

        public KeyVaultEncryptor(Uri uri, Algorithm algorithm = Algorithm.Rsa15)
                          : this(uri, new ManagedIdentityCredential(), algorithm)
        {
        }

        public KeyVaultEncryptor(Uri uri, TokenCredential credential, Algorithm algorithm = Algorithm.Rsa15)
        {
            _client = new CryptographyClient(uri, credential);

            switch(algorithm)
            {
                case Algorithm.Rsa15:      _algorithm = EncryptionAlgorithm.Rsa15;       break;
                case Algorithm.RsaOaep:    _algorithm = EncryptionAlgorithm.RsaOaep;     break;
                case Algorithm.RsaOaep256: _algorithm = EncryptionAlgorithm.RsaOaep256;  break;
            }
        }

        public enum Algorithm
        { 
            Rsa15,
            RsaOaep,
            RsaOaep256
        }

        #region IEncryptor

        public async Task<byte[]> Encrypt(byte[] aData)
        {
            var result = await _client.EncryptAsync(_algorithm, aData);

            return result.Ciphertext;
        }

        public async Task<byte[]> Decrypt(byte[] aEncrypted, int offset = 0)
        {
            var result = await _client.DecryptAsync(_algorithm, aEncrypted);

            return result.Plaintext;
        }

        public async Task Encrypt(Stream input, Stream output)
        {
            var length = input.Length;
            var buffer = new byte[length];

            await input.ReadAsync(buffer, 0, (int)length);

            var result = await Encrypt(buffer);

            await output.WriteAsync(result, 0, result.Length);

            if(output.CanSeek)
                output.Seek(0, SeekOrigin.Begin);
        }

        public async Task Decrypt(Stream input, Stream output)
        {
            var length = input.Length;
            var buffer = new byte[length];

            await input.ReadAsync(buffer, 0, (int)length);

            var result = await Decrypt(buffer);

            await output.WriteAsync(result, 0, result.Length);

            if(output.CanSeek)
                output.Seek(0, SeekOrigin.Begin);
        }
 
        #endregion
    }
}
