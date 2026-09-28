using CommandLine;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.PrismatCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var initialLogger = InitialLogger.Create<Program>();

        try
        {
            bool failedToParseArguments = false;
            SimulateOptions? simulationOptions = null;
            Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
                .WithParsed<SimulateOptions>(o => { simulationOptions = o; })
                .WithParsed<ProductionOptions>(_ => { })
                .WithNotParsed(errors =>
                {
                    initialLogger.LogError("Failed to parse command line arguments: {Errors}", string.Join(", ", errors));
                    failedToParseArguments = true;
                });

            if (failedToParseArguments)
            {
                return 1;
            }

            var host = PrismatCommunicationHostBuilder.CreateHostBuilder(args, simulationOptions, initialLogger)
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PrismatCommunicationDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            host.Run();
            return 0;
        }
        catch (Exception ex)
        {
            initialLogger.LogError(ex, "Unhandled exception");
            return 1;
        }
    }
}