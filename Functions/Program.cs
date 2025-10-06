using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        //logging
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // HttpClient for queue operations
        services.AddHttpClient();
        
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("ABCRetailers Functions starting up...");
            return new object();
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.AddDebug();
    })
    .Build();

host.Run();