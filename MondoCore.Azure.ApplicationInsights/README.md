# MondoCore.ApplicationInsights
  Class for logging telemetry to Azure Application Insights
 
<br>


#### Dependency Injection

Best practice for using the ApplicationInsights class is to use it as a provider for the Log class (in MondoCore.Log)

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Register the main Log class as a singleton.
            builder.Services.AddSingleton<ILog>( (p)=> 
            {
                var log = new Log();

                // Use GetService to get the TelemetryConfiguration to share with the host
                log.Register( new ApplicationInsights( p.GetService<TelemetryConfiguration>() ) );

                return log;
            );

            // Register IRequestLog as scoped. That same instance will be returned within the scope of a function invocation
            builder.Services.AddScoped<IRequestLog, RequestLog>();
        }
    }

<br>

License
----

MIT
