using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.WCSCommunication.CmdOptions;
using Gebhardt.Acc.WCSCommunication.Configuration;
using Gebhardt.Acc.WCSCommunication.Domain.Repositories;
using Gebhardt.Acc.WCSCommunication.Domain.Services;
using Gebhardt.Acc.WCSCommunication.Messages.Commands;
using Gebhardt.Acc.WCSCommunication.Persistence.DataAccess;
using Gebhardt.Acc.WCSCommunication.Persistence.Repositories;
using Gebhardt.Acc.WCSCommunication.Services.CommandHandler;
using Gebhardt.Acc.WCSCommunication.Services.EventHandler;
using Gebhardt.Acc.WCSCommunication.WcsApi.DbInterface;
using Gebhardt.Acc.WCSCommunication.WcsApi.SimulationMode;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace Gebhardt.Acc.WCSCommunication;

/// <summary>
/// Contains the host building logic for WCSCommunication.
/// </summary>
public static class WcsCommunicationHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="isSimulation">If this host builder is building a simulated environment.</param>
    /// <param name="simulateOptions">The simulation options.</param>
    public static IHostBuilder CreateHostBuilder(string[] args, bool isSimulation, SimulateOptions? simulateOptions)
    {
        var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
            .ConfigureServices((context, services) =>
            {
                if (isSimulation)
                {
                    services.AddHostedService<PollingBackgroundService<SimulationHostOrderService>>();

                    services.AddSingleton(_ => simulateOptions!);
                    services.AddSingleton<SimulationHostOrderService>();
                    services.AddTransient<IHostCommunicator, SimulationHostCommunicator>();
                    services.AddOptions<MassTransitHostOptions>().Configure(options => { options.WaitUntilStarted = true; });
                }
                else
                {
                    services.AddDbContextFactory<WcsHostDbContext>(builder =>
                    {
                        string? connectionString = context.Configuration.GetConnectionString(WcsHostDbContext.ConfigSectionName);
                        builder.UseSqlServer(connectionString, m =>
                        {
                            // EF Core 10: Avoid OPENJSON by using constants for parameterized collections
                            m.UseParameterizedCollectionMode(ParameterTranslationMode.Constant);
                        });
                    });
                    services.AddHostedService<PollingBackgroundService<WcsOrderHandler>>();
                    services.AddSingleton<WcsOrderHandler>();
                    services.AddTransient<IHostCommunicator, WcsDbCommunicator>();
                }
            });

        hostBuilder.ConfigureWebHost(host =>
        {
            host.UseKestrel()
                .ConfigureAppConfiguration((_, config) =>
                {
                    ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                    config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                })
                .Configure(app =>
                {
                    app.UseMetricServer();
                });
        });

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddDbContext<WcsCommunicationDbContext>(builder =>
            {
                string? connectionString = context.Configuration.GetConnectionString(WcsCommunicationDbContext.ConfigSectionName);
                builder.UseSqlServer(connectionString, m =>
                {
                    m.MigrationsHistoryTable(WcsCommunicationDbContext.MigrationTableName);
                    // EF Core 10: Avoid OPENJSON by using constants for parameterized collections
                    m.UseParameterizedCollectionMode(ParameterTranslationMode.Constant);
                });
            });
            services.AddTransient<IHostOrderRepository, HostOrderRepository>();
            services.ConfigureServicesMassTransit(context.Configuration, x =>
            {
                x.AddConsumer<ActionGroupEventHandler>();
                x.AddConsumer<HostOrderCommandHandler>(c =>
                {
                    var partitioner = c.CreatePartitioner(16);
                    partitioner.PartitionMessageById<CreateHostOrder, HostOrderCommandHandler>(c);
                    partitioner.PartitionMessageById<RemoveInvalidHostOrder, HostOrderCommandHandler>(c);
                    partitioner.PartitionMessageById<FinishHostOrder, HostOrderCommandHandler>(c);
                    partitioner.PartitionMessageById<FinishHostOrderCancellation, HostOrderCommandHandler>(c);
                    partitioner.PartitionMessageById<HandleCancellationFailure, HostOrderCommandHandler>(c);
                    partitioner.PartitionMessageById<HandleHostOrderStart, HostOrderCommandHandler>(c);
                    partitioner.PartitionMessageById<HandleHostOrderFailure, HostOrderCommandHandler>(c);
                });
            }, (ctx, cfg) => { cfg.UseConsumeFilter(typeof(ActionEventConsumeFilter<>), ctx); });
        });
        return hostBuilder;
    }
}