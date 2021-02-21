# MondoCore.Azure.KeyVault
  Encryption and secret storage using Azure KeyVault
 
<br>


#### Dependency Injection

Best practice is to inject the classes into your app

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Encryption. Url is specific to your KeyVault instance. This constructor requires usage of a managed identity.
            builder.Services.AddSingleton<IEncryptor>( return new KeyVaultEncryptor("https://mykeyvault.vault.azure.net/") );
            
            // Secret storage. Url is specific to your KeyVault instance. This constructor requires usage of a managed identity.
            builder.Services.AddSingleton<IBlobStore>( return new KeyVaultBlobStore(new Uri("https://mykeyvault.vault.azure.net/")) );
        }
    }

    public class MyClass 
    {
        public MyClass(IBlobStore secretStorage, IEncryption encryptor)
        {
            var secret = secretStorage.Get("mysecret");
            var cipherText = encryptor.Encrypt("somevalue");
        }
    }

<br>

License
----

MIT
