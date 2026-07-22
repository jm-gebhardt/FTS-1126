using CommandLine;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder;

/// <summary>
/// Swagger-compatible host factory used by Swashbuckle CLI.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public partial class Program
{
    /// <summary>
    /// Creates the host builder for CLI-based OpenAPI discovery.
    /// </summary>
    [PublicAPI]
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var initialLogger = InitialLogger.Create<Program>();

        var swaggerCli = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "SwaggerCli");

        if (swaggerCli)
        {
            var options = new ProductionOptions();
            var hostBuilder =
                TransportOrderHostBuilder.CreateHostBuilder(args, options, initialLogger, isSimulation: false);
            hostBuilder.UseDefaultServiceProvider((_, providerOptions) =>
            {
                providerOptions.ValidateScopes = false;
                providerOptions.ValidateOnBuild = false;
            });
            return hostBuilder;
        }

        ProductionOptions? productionOptions = null;
        SimulationOptions? simulationOptions = null;

        Parser.Default.ParseArguments<ProductionOptions, SimulationOptions>(args)
            .WithParsed<ProductionOptions>(options => { productionOptions = options; })
            .WithParsed<SimulationOptions>(options => { simulationOptions = options; });

        if (productionOptions != null)
        {
            return TransportOrderHostBuilder.CreateHostBuilder(args, productionOptions, initialLogger,
                isSimulation: false);
        }

        if (simulationOptions != null)
        {
            return TransportOrderHostBuilder.CreateHostBuilder(args, simulationOptions, initialLogger,
                isSimulation: true);
        }

        throw new InvalidOperationException(
            "No valid host mode was provided. Expected ProductionOptions or SimulationOptions.");
    }
}