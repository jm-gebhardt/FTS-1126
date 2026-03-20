using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.OrderTranslator.CmdOptions;
using Gebhardt.Acc.OrderTranslator.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Gebhardt.Acc.OrderTranslator.Configuration;

/// <summary>
/// Contains the host building logic for OrderTranslator.
/// </summary>
public static class OrderTranslatorHostBuilder
{
    /// <summary>
    /// Health Endpoint name 
    /// </summary>
    private const string HealthEndpoint = "/health";

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="simulateOptions">Simulation options if any.</param>
    /// <param name="initialLogger">The initial logger.</param>
    public static IHostBuilder CreateHostBuilder(string[] args, SimulateOptions? simulateOptions, ILogger initialLogger)
    {
        var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
            .AddMassTransit(initialLogger)
            .AddOrderTranslator()
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
                        app.UseHealthChecks(HealthEndpoint);
                    }));

        hostBuilder.ConfigureServices(s =>
        {
            s.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            s.AddHealthChecks()
                .AddCheck<StartupHealthCheck>("Startup",  tags: new[] { "ready" });
        });
        return hostBuilder;
    }
}