using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Core.Hosting.Configuration;
using Gebhardt.Core.WebApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;

namespace Gebhardt.Acc.PrismatCommunication.Configuration;

/// <summary>
/// Contains the host building logic for PrismatCommunication.
/// </summary>
public static class PrismatCommunicationHostBuilder
{
    private const string QuartzSchedulerName = "HousekeepingPrismatCommunication";
    private const string QuartzSchedulerId = "AUTO";

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
            .ConfigureServices((_, services) =>
            {
                services.AddQuartz(q =>
                {
                    q.SchedulerId = QuartzSchedulerId;
                    q.SchedulerName = QuartzSchedulerName;
                    q.UseTimeZoneConverter();
                });
                services.AddQuartzHostedService();
            })
            .ConfigureWebHost(h =>
                h.UseKestrel()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddCoreConfiguration(context.HostingEnvironment.EnvironmentName);
                        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                        config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                    })
                    .Configure(app =>
                    {
                        app.UseMetricServer();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHealthProbes();
                        });
                        if (simulationOptions != null)
                        {
                            app.UseDeveloperExceptionPage();
                        }
                    }));
    }
}