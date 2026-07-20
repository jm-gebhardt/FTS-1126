using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;

/// <summary>
/// Provides host building logic.
/// </summary>
public static class DeviceCommunicationVda5050SimulatorHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="initialLogger">The initial logger.</param>
    public static IHostBuilder CreateHostBuilder(string[] args, ILogger initialLogger)
    {
        return GebhardtStartup.CreateHostBuilder(args)
            .AddMassTransit(initialLogger)
            .AddSimulator(args, initialLogger)
            .ConfigureWebHost(host =>
            {
                host.UseKestrel()
                    .AddSimulatorWebApi()
                    .ConfigureAppConfiguration((_, config) =>
                    {
                        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                        config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                    })
                    .Configure(app =>
                    {
                        app.UseMetricServer();
                        app.UseSimulatorWebApi();
                        app.UseDeveloperExceptionPage();
                    });
            });
    }
}