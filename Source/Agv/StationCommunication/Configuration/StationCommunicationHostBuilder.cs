using AutoMapper.Internal;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.StationCommunication.CmdOptions;
using Gebhardt.Core.Hosting.Configuration;
using Gebhardt.Core.WebApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Gebhardt.Acc.StationCommunication.Configuration;

/// <summary>
/// Provides the host building logic for the service.
/// </summary>
public static class StationCommunicationHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="simulateOptions">Simulation options if any.</param>
    /// <param name="initialLogger">The initial logger.</param>
    public static IHostBuilder CreateHostBuilder(string[] args, SimulateOptions? simulateOptions, ILogger initialLogger)
    {
        var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddCoreConfiguration(context.HostingEnvironment.EnvironmentName);
            })
            .AddMassTransit(initialLogger)
            .AddStationCommandHandler(simulateOptions)
            .ConfigureWebHost(h =>
                h.UseKestrel()
                    .ConfigureAppConfiguration((_, config) =>
                    {
                        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                        config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                    })
                    .Configure(app =>
                    {
                        if (simulateOptions != null)
                        {
                            app.UseDeveloperExceptionPage();
                        }
                        app.UseMetricServer();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHealthProbes();
                        });
                    }));

        hostBuilder.ConfigureServices(s =>
            s.AddAutoMapper(cfg => cfg.Internal().ForAllMaps((_, map) => map.MaxDepth(10)), AppDomain.CurrentDomain.GetAssemblies())
        );
        return hostBuilder;
    }
}