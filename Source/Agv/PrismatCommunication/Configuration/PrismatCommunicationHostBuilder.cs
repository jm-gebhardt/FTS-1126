using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Gebhardt.Acc.PrismatCommunication.Configuration;

/// <summary>
/// Contains the host building logic for PrismatCommunication.
/// </summary>
public static class PrismatCommunicationHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="simulationOptions">The simulation options if any.</param>
    /// <param name="initialLogger">The initial logger.</param>
    /// <returns></returns>
    public static IHostBuilder CreateHostBuilder(string[] args, SimulateOptions? simulationOptions, ILogger initialLogger)
    {
        return GebhardtStartup.CreateHostBuilder(args)
            .AddMassTransit(initialLogger, simulationOptions)
            .AddPrismatCommunication(simulationOptions, initialLogger)
            .ConfigureWebHost(h =>
                h.UseKestrel()
                    .ConfigureAppConfiguration((_, config) =>
                    {
                        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                        config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                    })
                    .Configure(app =>
                    {
                        app.UseMetricServer();
                        if (simulationOptions != null)
                        {
                            app.UseDeveloperExceptionPage();
                        }
                    }));
    }
}