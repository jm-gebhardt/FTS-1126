using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.WebApi.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Gebhardt.Core.Hosting.Configuration;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Configuration;

/// <summary>
/// Provides host building logic.
/// </summary>
public static class DeviceCommunicationVda5050HostBuilder
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
            .AddDeviceCommunicationVda5050()
            .ConfigureWebHost(host =>
            {
                host.UseKestrel()
                    .AddDeviceCommunicationVda5050WebApi()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddCoreConfiguration(context.HostingEnvironment.EnvironmentName);
                        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                        config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                    })
                    .Configure(app =>
                    {
                        app.UseMetricServer();
                        app.UseDeviceCommunicationVda5050WebApi();
                        app.UseDeveloperExceptionPage();
                    });
            });
    }
}