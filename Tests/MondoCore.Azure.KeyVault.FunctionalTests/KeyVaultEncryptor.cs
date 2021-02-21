using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MondoCore.Common;
using MondoCore.Security;
using MondoCore.Security.Encryption;
using MondoCore.Azure.TestHelpers;

namespace MondoCore.Azure.KeyVault.FunctionalTests
{
    [TestClass]
    public class KeyVaultEncryptorTests
    {
        private const string data = "This once recognized, our attention naturally turns to the question of how small the payment can be. We know that by means of a wheel and axle we can raise 1,000 kilogrammes through 1 metre by allowing 100 kilogrammes to fall 10 metres.";

        [TestMethod]
        public async Task KeyVaultEncryptor_EncryptDecrypt_stream()
        {
            var encr = CreateEncryptor();

            var input = new MemoryStream();
            var sb = new StringBuilder();

            // Initialize input with some data (> buffer size which is 64k)
            for(var i = 0; i < 1; ++i)
            { 
                await input.WriteAsync(data);
                sb.Append(data);
            }

            input.Seek(0, SeekOrigin.Begin);

            await TestEncryptDecrypt(encr, input, sb.ToString());
        }
    
        [TestMethod]
        public async Task KeyVaultEncryptor_EncryptDecrypt_stream_small()
        {
            var encr = CreateEncryptor();
            
            using(var input = new MemoryStream())
            { 
                // Initialize input with some data 
                await input.WriteAsync("bobsyouruncle");

                input.Seek(0, SeekOrigin.Begin);

                await TestEncryptDecrypt(encr, input, "bobsyouruncle");
            }
        }

        [TestMethod]
        public async Task KeyVaultEncryptor_EncryptDecrypt_str()
        {
            var encr = CreateEncryptor();
            
            var result = await encr.Encrypt("bobsyouruncle");

            Assert.AreNotEqual("bobsyouruncle", result);
            Assert.AreEqual("bobsyouruncle", await encr.Decrypt(result));

        }
    
        private async Task TestEncryptDecrypt(IEncryptor encr, Stream input, string inputData)
        {
            using(var encrOutput = new MemoryStream())
            { 
                await encr.Encrypt(input, encrOutput);

                Assert.AreNotEqual(0, encrOutput.Length);
                Assert.IsTrue(encr.Policy.IsReadOnly);

                using(var decrOutput = new MemoryStream())
                {
                    await encr.Decrypt(encrOutput, decrOutput);

                    var decrResult = await decrOutput.ReadStringAsync();

                    Assert.AreEqual(inputData, decrResult);
                }
            }
        }

        private IEncryptor CreateEncryptor()
        { 
            var config = TestConfiguration.Load();
            var keyId = "https://jimsvault.vault.azure.net/keys/JimsKey/94445d4ff3104b5094006c75730b27ec";

            return new KeyVaultEncryptor(new Uri(keyId),
                                         config.KeyVaultTenantId, 
                                         config.KeyVaultClientId, 
                                         config.KeyVaultClientSecret);
        }
    }
}
