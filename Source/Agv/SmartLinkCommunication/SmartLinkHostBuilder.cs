using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.SmartLinkCommunication.Models;
using Gebhardt.Acc.SmartLinkCommunication.Services;
using Gebhardt.Core.Hosting.Configuration;
using Gebhardt.Core.WebApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using RestSharp;

namespace Gebhardt.Acc.SmartLinkCommunication;

/// <summary>
/// Contains the host building logic for SmartLinkCommunication.
/// </summary>
public static class SmartLinkHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="config">The configuration.</param>
    public static IHostBuilder CreateHostBuilder(string[] args, Config config)
    {
        var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
            .ConfigureAppConfiguration((context, conf) =>
            {
                conf.AddCoreConfiguration(context.HostingEnvironment.EnvironmentName);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddScoped<IIoActionExecutionService, IoActionExecutionService>();
                services.AddScoped<IRestClient, RestClient>();
                services.AddScoped<ISmartLinkService, SmartLinkService>();
                services.AddScoped<ISmartLinkResponseHandler, SmartLinkResponseHandler>();
                services.AddScoped<IIoStateTrackerService, IoStateTrackerService>();
                services.AddSingleton<IModuleScopeFactory, ModuleScopeFactory>();
                services.AddScoped<IModuleConfigProvider, ModuleConfigProvider>();

                foreach (var moduleConfig in config.Modules)
                {
                    // we must construct the PollingBackgroundService manually, as there is no other way to inject  this specific instance of the moduleConfig
                    services.AddHostedService(sp => new PollingBackgroundService<ModulePollingService>(
                        sp.GetRequiredService<ILogger<PollingBackgroundService<ModulePollingService>>>(),
                        new ModulePollingService(sp, moduleConfig)
                    ));
                }

                services.ConfigureServicesMassTransit(context.Configuration);
            })
            .ConfigureWebHost(h => h.UseKestrel()
                .ConfigureAppConfiguration((_, c) =>
                {
                    ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                    c.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                })
                .Configure(app =>
                {
                    app.UseMetricServer();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHealthProbes();
                    });
                })
            );

        return hostBuilder;
    }
}