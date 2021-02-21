# MondoCore.Azure.Configuration
  Configuration (settings) using Azure App Configuration
 
<br>


#### Dependency Injection

Best practice for using the AzureConfiguration class is to inject it into your app

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // The Url is specific to your Azure Configuration resource. This constructor implies use of security using a managed identity.
            builder.Services.AddSingleton<ISettings>( return new AzureConfiguration("https://myconfig.azconfig.io") );
        }
    }

    public class MyClass 
    {
        public MyClass(ISettings settings)
        {
            var maxRetries = settings.Get<int>("MaxRetries"); // Extension method for using generics

            ...
        }
    }

<br>

License
----

MIT
