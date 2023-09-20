using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;

namespace Unosquare.ModelValidation.Playground;

internal class Program
{
    static async Task Main(string[] args)
    {
        Logger? logger = null;

        var builder = Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((services) =>
            {
                services
                    .AddLocalization(options => options.ResourcesPath = "Resources")
                    .AddSingleton<Sample>();
            })
            .ConfigureLogging((context, builder) =>
            {
                logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(context.Configuration)
                    .CreateLogger();

                builder
                    .ClearProviders()
                    .AddSerilog(logger, false);
            });

        var host = builder.Build();

        try
        {
            var worker = host.Services.GetRequiredService<Sample>();
            await worker.RunAsync().ConfigureAwait(false);
        }
        finally
        {
            // We need to dispose of the logger;
            // since we are not calling the host.RunAsync() method,
            // and there is a file buffer that has not been written to,
            // we need to manually call Dispose on the logger.
            logger?.Dispose();
        }

        Console.WriteLine("Host finshed running. Press any key to exit.");
        Console.ReadKey(intercept: true);
    }
}