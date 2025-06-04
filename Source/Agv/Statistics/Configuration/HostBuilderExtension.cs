using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.SemaphoreProviders;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Filters;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;
using Gebhardt.Acc.Statistics.Services.BackgroundServices;
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
                services.AddHostedService<HousekeepingStartup>();
                services.ConfigureServicesMassTransit(context.Configuration, x =>
                {
                    x.AddBusConsumers();

                    x.AddMediator(c => c.AddMediatorConsumers());
                }, rabbitMqConfiguration: (ctx, cfg) =>
                {
                    cfg.UseConsumeFilter<AgvTracingConsumeFilters>(ctx);
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
                    m => m.MigrationsHistoryTable(StatisticsDbContext.MigrationTableName)));
            services.AddTransient<IStatisticsDb, EfStatisticsDb>();
            services.AddTransient<IMapRepository, EfMapRepository>();
            services.AddTransient<IGridRepository, EfGridRepository>();
            services.AddTransient<IAgvFieldViolationRepository, EfAgvFieldViolationRepository>();
            services.AddTransient<IAgvErrorRepository, EfAgvErrorRepository>();
            services.AddTransient<IGridAgvPositionRepository, EfGridAgvPositionRepository>();
            services.AddTransient<IMapAgvPositionsRepository, EfMapAgvPositionsRepository>();
            
            services.AddSingleton(typeof(IInMemoryCache<,>), typeof(InMemoryCache<,>));
            
            RegisterFilters(services);
        });
        return hostBuilder;
    }

    private static void RegisterFilters(IServiceCollection services)
    {
        services.AddSingleton(new AgvPositionConfiguration(0.5, Mathematics.ConvertToRadians(5), 1000) { MinInterval = TimeSpan.FromSeconds(3), MaxInterval = TimeSpan.FromSeconds(20) });
        services.AddSingleton<ISignificanceChecker<AgvPosition>, AgvPositionSignificanceChecker>();
        services.AddSingleton<IMessageFilter<AgvPositionReceived>, AgvPositionReceivedFilter>();

        services.AddSingleton<IConcurrentDictionaryProvider<IMessageFilter<AgvPositionReceived>>, AgvPositionReceivedDictionaryProvider>();
        services.AddTransient<IMessageFilter<AgvPositionReceived>, AgvPositionReceivedInMemoryFilter>();
        services.AddSingleton<ISemaphoreProvider<IAgvPositionsReceivedSemaphoreProvider>, SemaphoreProvider<IAgvPositionsReceivedSemaphoreProvider>>();
        services.AddSingleton<IConcurrentDictionaryProvider<ISemaphoreProvider<IAgvPositionsReceivedSemaphoreProvider>>, ConcurrentDictionaryProvider<ISemaphoreProvider<IAgvPositionsReceivedSemaphoreProvider>>>();
        services.AddSingleton<IKeyedSemaphoreProvider<IAgvPositionsReceivedSemaphoreProvider>, KeyedSemaphoreProvider<IAgvPositionsReceivedSemaphoreProvider>>();
        services.AddSingleton<IKeyedSemaphoreDictionaryProvider<IAgvPositionsReceivedSemaphoreProvider>, KeyedSemaphoreDictionaryProvider<IAgvPositionsReceivedSemaphoreProvider>>();
    }
}