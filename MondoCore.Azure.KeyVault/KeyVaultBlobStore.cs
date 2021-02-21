using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using MondoCore.Common;

namespace MondoCore.Azure.KeyVault
{
    public class KeyVaultBlobStore : IBlobStore
    {
        private readonly SecretClient _client;

        public KeyVaultBlobStore(Uri uri, string tenantId, string clientId, string secret) : this(uri, new ClientSecretCredential(tenantId, clientId, secret))
        {
        }

        public KeyVaultBlobStore(Uri uri) : this(uri, new ManagedIdentityCredential())
        {
        }

        public KeyVaultBlobStore(Uri uri, TokenCredential credential)
        {
            _client = new SecretClient(uri, credential);
        }

        #region IBlobStore

        public async Task Delete(string id)
        {
            try
            { 
                await _client.StartDeleteSecretAsync(id);
            }
            catch(RequestFailedException rex) when (rex.Status == 404)
            {
                // Do nothing
            }
        }

        public Task Enumerate(string filter, Func<IBlob, Task> fnEach, bool asynchronous = true)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> Find(string filter)
        {
            throw new NotImplementedException();
        }

        public async Task<string> Get(string id, Encoding encoding = null)
        {
            try
            { 
                var secret = await _client.GetSecretAsync(id);

                return secret?.Value?.Value;
            }
            catch(RequestFailedException rex) when (rex.Status == 404)
            { 
                throw new FileNotFoundException("Secret not found", rex);
            }
        }

        public async Task Get(string id, Stream destination)
        {
            var result = await Get(id);

            await destination.WriteAsync(result);
        }

        public async Task<byte[]> GetBytes(string id)
        {
            var result = await Get(id);

            return UTF8Encoding.UTF8.GetBytes(result);
        }

        public Task<Stream> OpenRead(string id)
        {
            throw new NotSupportedException();
        }

        public Task<Stream> OpenWrite(string id)
        {
            throw new NotSupportedException();
        }

        public Task Put(string id, string content, Encoding encoding = null)
        {
            var secret = new KeyVaultSecret(id, content);

            return _client.SetSecretAsync(secret);
        }

        public async Task Put(string id, Stream content)
        {
            await Put(id, await content.ReadStringAsync());
        }

        #endregion
    }
}
