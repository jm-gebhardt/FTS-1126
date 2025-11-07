using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.Common.Messages.Filters;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Extensions;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Events;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;
using Gebhardt.Acc.Statistics.Services.BackgroundServices;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Configuration;

/// <summary>
/// Provides extension method for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtension
{
    private static void ConfigureGridPositionAttachedEventHandler<THandler, TMessage>(IRabbitMqReceiveEndpointConfigurator e, IBusRegistrationContext ctx, IPartitioner partitioner)
        where THandler : class, IConsumer
        where TMessage : class, IAgvSerialNumber, IWithAgvPosition, IMessageCreatedDate
    {
        e.ConfigureConsumer<THandler>(ctx, cc =>
        {
            cc.Message<TMessage>(x =>
                x.UsePartitioner(partitioner, m => m.Message.SerialNumber));
        });
        e.ConfigureConsumer<GridPositionPersistedForwarder<TMessage>>(ctx, cc =>
        {
            cc.Message<GridPositionPersisted>(x =>
                x.UsePartitioner(partitioner, m => m.Message.SerialNumber));
        });
    }

    /// <summary>
    /// Add MassTransit configuration.
    /// </summary>
    public static IHostBuilder AddMassTransit(this IHostBuilder hostBuilder, ILogger initialLogger)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            try
            {
                services.AddHostedService<InitialGridProvider>();
                services.AddHostedService<Housekeeping>();
                services.ConfigureServicesMassTransit(context.Configuration, x =>
                {
                    x.AddBusConsumers();

                    x.AddMediator(c => c.AddMediatorConsumers());
                }, rabbitMqConfiguration: (ctx, cfg) =>
                {
                    cfg.UseConsumeFilter<AgvTracingConsumeFilters>(ctx);

                    cfg.ReceiveEndpoint("agv-events", e =>
                    {
                        // we need one queue and one partitioner for the AGVs, to ensure that all messages for the same AGV are processed in order
                        var partitioner = e.CreatePartitioner(16);

                        e.ConfigureConsumer<AgvPositionReceivedHandler>(ctx, cc =>
                        {
                            cc.Message<AgvPositionReceived>(x =>
                                x.UsePartitioner(partitioner, m => m.Message.SerialNumber));
                            cc.Message<ForcePersistAgvPosition>(x =>
                                x.UsePartitioner(partitioner, m => m.Message.SerialNumber));
                        });

                        ConfigureGridPositionAttachedEventHandler<AgvErrorEventHandler, AgvErrorReceived>(e, ctx, partitioner);
                        ConfigureGridPositionAttachedEventHandler<WifiMetricsEventHandler, AgvWifiMetricsReceived>(e, ctx, partitioner);
                    });
                });
            }
            catch (InvalidOperationException e)
            {
                initialLogger.LogError(e, "MassTransit could not be started");
                throw;
            }
        });

        return hostBuilder;
    }

    /// <summary>
    /// Add service specific configurations.
    /// </summary>
    public static IHostBuilder AddStatistics(this IHostBuilder hostBuilder)
    {
        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddConfigurationTypeManager();
            services.AddDbContext<StatisticsDbContext>(o =>
                o.UseSqlServer(context.Configuration.GetConnectionString(StatisticsDbContext.ConfigSectionName),
                    m => m.MigrationsHistoryTable(IDbContext<StatisticsDbContext>.MigrationTableName).EnableRetryOnFailure()));
            services.AddTransient<IStatisticsDb, EfStatisticsDb>();
            services.AddTransient<IMapRepository, EfMapRepository>();
            services.AddTransient<IHouseKeepingRepository, EfHousekeepingRepository>();
            services.AddTransient<IGridRepository, EfGridRepository>();
            services.AddTransient<IAgvFieldViolationRepository, EfAgvFieldViolationRepository>();
            services.AddTransient<IAgvErrorRepository, EfAgvErrorRepository>();
            services.AddTransient<IGridAgvPositionRepository, EfGridAgvPositionRepository>();
            services.AddTransient<IMapAgvPositionsRepository, EfMapAgvPositionsRepository>();
            services.AddTransient<IAgvWifiMetricsRepository, EfAgvWifiMetricsRepository>();

            services.AddSingleton(typeof(ICache<,>), typeof(Cache<,>));
            services.AddAgvScopedCache<Guid>(1000, false);
            services.AddAgvScopedCache<AgvWifiMetricsReceived>(20);
            services.AddAgvScopedCache<AgvErrorReceived>(20);
            // we need to register it also here (next to the AddConsumer), because the GridPositionPersistedForwarder needs to resolve it via DI
            services.AddScoped<GridPositionAttachedHandlerBase<AgvErrorReceived>, AgvErrorEventHandler>();
            services.AddScoped<GridPositionAttachedHandlerBase<AgvWifiMetricsReceived>, WifiMetricsEventHandler>();
            
            services.AddHealthChecks()
                .AddDbContextCheck<StatisticsDbContext>();

            RegisterFilters(services);
        });
        return hostBuilder;
    }

    private static void RegisterFilters(IServiceCollection services)
    {
        // Agv position
        services.AddSingleton(new AgvPositionConfiguration(0, 0, 0) { MinInterval = TimeSpan.FromSeconds(5), MaxInterval = TimeSpan.FromSeconds(5) });
        services.AddSingleton<ISignificanceChecker<AgvPosition>, AgvPositionSignificanceChecker>();
        services.AddTransient<IMessageFilter<AgvPositionReceived>, AgvPositionReceivedKeyedFilter>();
        // Wi-Fi metrics
        services.AddSingleton(new WifiConfiguration(3, 10, 3) { MinInterval = TimeSpan.Zero, MaxInterval = TimeSpan.MaxValue });
        services.AddSingleton<ISignificanceChecker<WifiMetrics>, WifiMetricsSignificanceChecker>();
        services.AddTransient<IMessageFilter<AgvWifiMetricsReceived>, AgvWifiMetricsReceivedKeyedFilter>();

        // generic
        services.AddSingleton(typeof(IConcurrentDictionaryProvider<>), typeof(ConcurrentDictionaryProvider<>));
        services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));
        services.AddSingleton(typeof(IKeyedSemaphoreProvider<>), typeof(KeyedSemaphoreProvider<>));
        services.AddSingleton(typeof(IKeyedSemaphoreDictionaryProvider<>), typeof(KeyedSemaphoreDictionaryProvider<>));
    }
}