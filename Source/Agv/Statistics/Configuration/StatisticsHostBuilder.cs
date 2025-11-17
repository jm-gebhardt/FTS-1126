using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Statistics.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Quartz;

namespace Gebhardt.Acc.Statistics.Configuration;

/// <summary>
/// Contains the host building logic for Statistics.
/// </summary>
public static class StatisticsHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="initialLogger">The initial logger.</param>
    /// <param name="quartzSchedulerId">The id of the quartz scheduler.</param>
    /// <param name="quartzSchedulerName">The name of the quartz scheduler.</param>
    /// <returns></returns>
    public static IHostBuilder CreateHostBuilder(string[] args, ILogger initialLogger, string quartzSchedulerId, string quartzSchedulerName)
    {
        return GebhardtStartup.CreateHostBuilder(args)
            .AddMassTransit(initialLogger)
            .AddStatistics()
            .ConfigureServices((_, services) =>
            {
                services.AddQuartz(q =>
                {
                    q.SchedulerId = quartzSchedulerId;
                    q.SchedulerName = quartzSchedulerName;
                    q.UseTimeZoneConverter();
                });
                services.AddQuartzHostedService();
            })
            .ConfigureWebHost(h =>
                h.UseKestrel()
                    .ConfigureAppConfiguration((_, config) =>
                    {
                        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                        config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                    })
                    .AddStatisticsWebApi()
                    .Configure(app =>
                    {
                        app.UseMetricServer();
                        app.UseStatisticsWebApi();
                        app.UseDeveloperExceptionPage();
                    }));
    }
}