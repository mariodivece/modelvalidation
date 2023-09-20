using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Unosquare.ModelValidation.Playground;

internal class Program
{
    static async Task Main(string[] args)
    {
        
        var builder = Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddUserSecrets<Program>(true);
            })
            .ConfigureServices((services) =>
            {
                services
                    .AddLocalization(options => options.ResourcesPath = "Resources")
                    .AddTransient<Sample>();
            })
            .ConfigureLogging((context, builder) =>
            {
                var consoleLogger = new LoggerConfiguration()
                    .ReadFrom.Configuration(context.Configuration)
                    .CreateLogger();

                builder
                    .ClearProviders()
                    .AddSerilog(consoleLogger, true);
            });

        var host = builder.Build();

        var worker = host.Services.GetRequiredService<Sample>();
        await worker.RunAsync().ConfigureAwait(false);

        Console.WriteLine("Host finshed running. Press any key to exit.");
        Console.ReadKey(intercept: true);
    }
}