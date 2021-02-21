using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MondoCore.Common;
using MondoCore.Azure.TestHelpers;
using System.IO;

namespace MondoCore.Azure.KeyVault.FunctionalTests
{
    [TestClass]
    public class KeyVaultBlobStoreTests
    {
        [TestMethod]
        public async Task KeyVaultBlobStore_Get()
        {
            var store = CreateStore();

            Assert.AreEqual("Chevy", await store.Get("Make"));
        }

        [TestMethod]
        public async Task KeyVaultBlobStore_Put()
        {
            var store = CreateStore();
            var key = Guid.NewGuid().ToId();

            await store.Put(key, "blah");

            Assert.AreEqual("blah", await store.Get(key));

            await store.Delete(key);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task KeyVaultBlobStore_Delete()
        {
            var store = CreateStore();
            var key = Guid.NewGuid().ToId();

            await store.Put(key, "blah");

            Assert.AreEqual("blah", await store.Get(key));

            await store.Delete(key);

            Assert.AreEqual(null, await store.Get(key));
        }

        private IBlobStore CreateStore()
        { 
            var config = TestConfiguration.Load();

            return new KeyVaultBlobStore(new Uri(config.KeyVaultUri),
                                         config.KeyVaultTenantId, 
                                         config.KeyVaultClientId, 
                                         config.KeyVaultClientSecret);
        }
    }
}
