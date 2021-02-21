using Microsoft.VisualStudio.TestTools.UnitTesting;

using MondoCore.Common;
using MondoCore.Azure.Configuration;
using MondoCore.Azure.TestHelpers;

namespace MondoCore.Azure.Configuration.FunctionalTests
{
    [TestClass]
    public class AzureConfigurationTests
    {
        [TestMethod]
        public void AzureConfiguration_Get()
        {
            var config = CreateConfig();

            Assert.AreEqual("Chevy",        config.Get("Make"));
            Assert.AreEqual("Corvette" ,    config.Get("Model"));
            Assert.AreEqual("Black",        config.Get("Color"));
            Assert.AreEqual("1964",         config.Get("Year"));
        }

        private ISettings CreateConfig(string folder = "")
        { 
            var config = TestConfiguration.Load();

            return new AzureConfiguration(config.ConfigConnectionString,
                                          config.KeyVaultTenantId, 
                                          config.KeyVaultClientId, 
                                          config.KeyVaultClientSecret);
        }
    }
}
