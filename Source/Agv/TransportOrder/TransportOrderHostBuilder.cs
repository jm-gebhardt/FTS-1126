using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Core.Configurations;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Configurations;
using Gebhardt.Acc.TransportOrder.WebApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;

namespace Gebhardt.Acc.TransportOrder;

/// <summary>
/// Contains the host building logic for TransportOrder.
/// </summary>
public static class TransportOrderHostBuilder
{
    private const string QuartzSchedulerName = "HousekeepingTransportOrder";
    private const string QuartzSchedulerId = "AUTO";

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="options">The system options to use.</param>
    /// <param name="initialLogger">The initial logger.</param>
    /// <param name="isSimulation">If the host builder is building a simulated environment.</param>
    public static IHostBuilder CreateHostBuilder(string[] args, SystemOptions options, ILogger initialLogger, bool isSimulation)
    {
        var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
            .ConfigureServices((_, services) => services.AddTrafficManagementGraphInitializer())
            .AddMassTransit(initialLogger)
            .AddTransportOrderCore(options)
            .ConfigureServices((_, services) =>
            {
                services.AddQuartz(q =>
                {
                    q.SchedulerId = QuartzSchedulerId;
                    q.SchedulerName = QuartzSchedulerName;
                    q.UseTimeZoneConverter();
                });
                services.AddQuartzHostedService();
            });

        hostBuilder.ConfigureWebHost(host =>
        {
            host.UseKestrel()
                .AddTransportOrderWebApi(isSimulation)
                .ConfigureAppConfiguration((_, config) =>
                {
                    ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                    config.AddDefaultConfiguration(ConfigurationTypeManager.Instance);
                })
                .Configure(app =>
                {
                    app.UseMetricServer();
                    app.UseTransportOrderWebApi();
                    app.UseDeveloperExceptionPage();
                });
        });

        return hostBuilder;
    }
}