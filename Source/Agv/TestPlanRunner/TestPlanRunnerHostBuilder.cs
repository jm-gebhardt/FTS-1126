using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TestPlanRunner.Configuration;
using Gebhardt.Acc.TestPlanRunner.Providers;
using Gebhardt.Acc.TestPlanRunner.Services;
using Gebhardt.Acc.TestPlanRunner.Services.EventHandlers;
using Gebhardt.Acc.TestPlanRunner.Services.Parsing;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Gebhardt.Acc.TestPlanRunner;

/// <summary>
/// Contains the host building logic for TestPlanRunner.
/// </summary>
public static class TestPlanRunnerHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="initialLogger">The initial logger</param>
    public static IHostBuilder CreateHostBuilder(string[] args, ILogger initialLogger)
    {
        var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
            .ConfigureServices((context, services) =>
            {
                ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

                // Configuration
                string sectionName = ConfigurationTypeManager.Instance.GetSectionName<TestPlanRunnerConfiguration>();
                var config = context.Configuration.GetSection(sectionName).Get<TestPlanRunnerConfiguration>()
                             ?? new TestPlanRunnerConfiguration();
                services.AddSingleton(config);

                // Database
                services.AddDbContextFactory<TransportOrderDbContext>(builder =>
                {
                    string? connectionString = context.Configuration.GetConnectionString(TransportOrderDbContext.ConfigSectionName);
                    builder.UseSqlServer(connectionString);
                });

                // Parsers
                services.AddSingleton<JsonTestPlanParser>();
                services.AddSingleton<YamlTestPlanParser>();
                services.AddSingleton<ITestPlanParserFactory, TestPlanParserFactory>();

                // Services
                services.AddSingleton<IAgvExecutionStateProvider, AgvExecutionStateProvider>();
                services.AddSingleton<ITestPlanDictionaryProvider, TestPlanDictionaryProvider>();
                services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));
                services.AddSingleton(typeof(IConcurrentDictionaryProvider<>), typeof(ConcurrentDictionaryProvider<>));
                services.AddSingleton(typeof(IKeyedSemaphoreProvider<>), typeof(KeyedSemaphoreProvider<>));
                services.AddTransient<IAgvRepository, AgvRepository>();
                services.AddTransient<IActionGroupRepository, EfActionGroupRepository>();
                services.AddTransient<IAgvRouteRetrievalService, AgvRouteRetrievalService>();
                services.AddSingleton<ITestPlanManager, TestPlanManager>();
                services.AddSingleton<IActionCancellationEvaluator, ActionCancellationEvaluator>();

                // File watcher
                services.AddSingleton<TestPlanFileWatcher>();
                services.AddHostedService<PollingBackgroundService<TestPlanFileWatcher>>();

                // MassTransit
                try
                {
                    services.ConfigureServicesMassTransit(context.Configuration, x => { x.AddConsumer<TestPlanTriggerEventHandler>(); });
                }
                catch (InvalidOperationException e)
                {
                    initialLogger.LogError(e, "MassTransit could not be started");
                    throw;
                }
                services.ConfigureTracing(context.Configuration);
            });

        hostBuilder.ConfigureWebHost(host =>
        {
            host.UseKestrel()
                .ConfigureAppConfiguration((_, config) =>
                {
                    ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                    config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                })
                .Configure(app => { app.UseMetricServer(); });
        });

        return hostBuilder;
    }
}