using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Messages.Filters;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;
using Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices;
using Gebhardt.Acc.PrismatCommunication.Services.TelegramApi;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.PrismatCommunication.Messages.Filters;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Services;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.PrismatCommunication.Simulation;

namespace Gebhardt.Acc.PrismatCommunication.Configuration;

/// <summary>
/// Provides extension method for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtension
{
    private static readonly TimeSpan _busStartTimeout = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Add MassTransit configuration.
    /// </summary>
    public static IHostBuilder AddMassTransit(this IHostBuilder hostBuilder, ILogger initialLogger, SimulateOptions? simulateOptions = null)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            try
            {
                services.AddSingleton<ITimeProvider, DateTimeNowUtcProvider>();
                services.ConfigureServicesMassTransit(context.Configuration, x => { x.AddBusConsumers(simulateOptions != null); }, rabbitMqConfiguration: (ctx, cfg) =>
                {
                    cfg.UseConsumeFilter<ActionGroupTracingConsumeFilters>(ctx);
                    cfg.UseConsumeFilter<ActionGroupEventConsumeFilter>(ctx);
                });
                services.AddOptions<MassTransitHostOptions>()
                    .Configure(options =>
                    {
                        options.WaitUntilStarted = true;
                        options.StartTimeout = _busStartTimeout;
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
    /// <param name="hostBuilder">This <see cref="IHostBuilder"/>.</param>
    /// <param name="simulateOptions">The <see cref="SimulateOptions"/> if in simulation mode.</param>
    public static IHostBuilder AddPrismatCommunication(this IHostBuilder hostBuilder, SimulateOptions? simulateOptions)
    {
        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddConfigurationTypeManager();

            void ConfigureDbContextOptions(DbContextOptionsBuilder builder)
            {
                string? connectionString = context.Configuration.GetConnectionString(PrismatCommunicationDbContext.ConfigSectionName);
                builder.UseSqlServer(connectionString, m =>
                {
                    m.MigrationsHistoryTable(PrismatCommunicationDbContext.MigrationTableName);
                    m.UseCompatibilityLevel(120); // EF9 compatibility level
                });
            }

            services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));

            services.AddDbContext<PrismatCommunicationDbContext>(ConfigureDbContextOptions);
            services.AddDbContextFactory<PrismatCommunicationDbContext>(ConfigureDbContextOptions);

            services.AddTransient<IHostOrderRepository, EfHostOrderRepository>();
            services.AddTransient<ISystemStatusRepository, EfSystemStatusRepository>();
            services.AddTransient<IStationRepository, EfStationRepository>();
            services.AddTransient<IShortLoopWeTargetStationRepository, EfShortLoopWeTargetStationRepository>();
            services.AddTransient<ITelegramRepository, EfTelegramRepository>();
            services.TryAddKaeserGraphConfiguration(simulateOptions != null);
            services.AddSingleton<ObservableConcurrentQueue<PrismatTelegram>>();
            services.AddSingleton<TelegramQueuePersistenceService>();
            services.AddHostedService<InitializationProvider>();

            // Filters
            services.AddSingleton<ISemaphoreProvider, SemaphoreProvider>();
            services.AddSingleton<ISignificanceChecker<ActionGroupCompleted>, ActionGroupEventSignificanceChecker<ActionGroupCompleted>>();
            services.AddSingleton<UnTimedInMemoryFilter<ActionGroupCompleted>>();

            services.AddSingleton<ISignificanceChecker<ActionGroupStarted>, ActionGroupEventSignificanceChecker<ActionGroupStarted>>();
            services.AddSingleton<UnTimedInMemoryFilter<ActionGroupStarted>>();

            services.AddSingleton<ISignificanceChecker<PickActionCompleted>, ActionGroupEventSignificanceChecker<PickActionCompleted>>();
            services.AddSingleton<UnTimedInMemoryFilter<PickActionCompleted>>();

            services.AddTransient<IAklAgvLimitGuard, AklAgvLimitGuard>();

            services.RegisterValidators();

            if (simulateOptions == null)
            {
                services.TryAddPrismatApi(context.Configuration);
                services.AddSingleton<IHostCommunicator, TelegramHostCommunicator>();
                return;
            }

            services.AddSingleton<IHostCommunicator, SimulationHostCommunicator>();
            services.AddSingleton(_ => simulateOptions);

            services.AddHostOrderCreator<LongLoopSimulator>();
            switch (simulateOptions.AplLoopOnly)
            {
                case false:
                    services.AddHostOrderCreator<ShortLoopSimulator>()
                        .AddHostOrderCreator<InputLoopSimulator>();
                    break;

                case true:
                    services.AddHostOrderCreator<LongLoopInputSimulator>();
                    break;
            }
        });
        return hostBuilder;
    }

    private static IServiceCollection AddHostOrderCreator<T>(this IServiceCollection services)
        where T : class, IHostOrderCreator
    {
        services.AddHostedService<PollingBackgroundService<T>>();
        services.AddSingleton<T>();
        services.AddSingleton<IHostOrderCreator, T>(sp => sp.GetRequiredService<T>());

        return services;
    }
}