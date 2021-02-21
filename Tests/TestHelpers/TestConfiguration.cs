using System;
using System.IO;
using System.Reflection;

using MondoCore.Common;

using Newtonsoft.Json;

namespace MondoCore.Azure.TestHelpers
{
    public static class TestConfiguration
    {
        public static Configuration Load()
        { 
            var path = Assembly.GetCallingAssembly().Location.SubstringBefore("\\bin");
            var json = File.ReadAllText(Path.Combine(path, "localhost.json"));

            return JsonConvert.DeserializeObject<Configuration>(json);
        }
    }

    public class Configuration
    {
        public string InstrumentationKey        { get; set; }
        public string ConnectionString          { get; set; }
        public string DataLakeConnectionString          { get; set; }
        public string ConfigConnectionString    { get; set; }

        public string KeyVaultUri               { get; set; }
        public string KeyVaultTenantId          { get; set; }
        public string KeyVaultClientId          { get; set; }
        public string KeyVaultClientSecret      { get; set; }     
    }
}
