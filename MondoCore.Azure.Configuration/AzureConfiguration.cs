using System;

using Azure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

using MondoCore.Common;

namespace MondoCore.Azure.Configuration
{
    /// <summary>
    /// Access configuration entries in Azure App Configuration
    /// </summary>
    public class AzureConfiguration : ISettings
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Connect to Azure App Configuration using a url and managed indenity for app running in Azure
        /// </summary>
        /// <param name="azureConfigUrl"></param>
        public AzureConfiguration(string azureConfigUrl)
        {
            var builder    = new ConfigurationBuilder();
            var credential = new DefaultAzureCredential();

            builder.AddAzureAppConfiguration(options =>
            {
                options.Connect(new Uri(azureConfigUrl), credential);

                options.ConfigureKeyVault(kv =>
                {
                    kv.SetCredential(credential);
                });
            });

            _config = builder.Build();
        }

        /// <summary>
        /// Connect to Azure App Configuration using a connection string and tenant id. For debugging and testing locally only.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tenantId"></param>
        /// <param name="clientId"></param>
        /// <param name="secret"></param>
        public AzureConfiguration(string connectionString, string tenantId, string clientId, string secret)
        {
            var builder = new ConfigurationBuilder();

            builder.AddAzureAppConfiguration(options =>
            {
                options.Connect(connectionString)
                       .ConfigureKeyVault(kv =>
                        {
                            kv.SetCredential(new ClientSecretCredential(tenantId, clientId, secret));
                        });
            });

            _config = builder.Build();
        }

        #region ISettings

        public string Get(string key)
        {
            return _config[key];
        }

        #endregion
    }
}
