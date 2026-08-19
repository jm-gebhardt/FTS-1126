using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Filters;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Messages.Filters;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;
using Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices;
using Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices.Housekeeping;
using Gebhardt.Acc.PrismatCommunication.Services.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.Simulation;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Services;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Core.EntityFrameworkCore;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Configuration;

/// <summary>
/// Provides extension method for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtension
{
    private static readonly TimeSpan _busStartTimeout = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Extension block for <see cref="IHostBuilder"/>.
    /// </summary>
    extension(IHostBuilder hostBuilder)
    {
        /// <summary>
        /// Add MassTransit configuration.
        /// </summary>
        public IHostBuilder AddMassTransit(ILogger initialLogger, SimulateOptions? simulateOptions = null)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                try
                {
                    services.AddSingleton(TimeProvider.System);
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
                    services.ConfigureTracing(context.Configuration);
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
        /// <param name="simulateOptions">The <see cref="SimulateOptions"/> if in simulation mode.</param>
        /// <param name="logger">Logger instance</param>
        public IHostBuilder AddPrismatCommunication(SimulateOptions? simulateOptions, ILogger logger)
        {
            ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

            hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddConfigurationTypeManager();

                void ConfigureDbContextOptions(DbContextOptionsBuilder builder)
                {
                    string? connectionString = context.Configuration.GetConnectionString(DbProviders.SqlServer)  
                                               ??context.Configuration.GetConnectionString(PrismatCommunicationDbContext.ConfigSectionName);
                    builder.UseSqlServer(connectionString, m =>
                    {
                        m.MigrationsHistoryTable(IDbContext<PrismatCommunicationDbContext>.MigrationTableName);
                        m.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                    builder.AddInterceptors(new EfCommandMetricsInterceptor());
                }

                services.AddHostedService<InitializationProvider>();
                services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));

                services.AddDbContextFactory<PrismatCommunicationDbContext>(ConfigureDbContextOptions);

                services.AddTransient<IHostOrderRepository, EfHostOrderRepository>();
                services.AddTransient<ISystemStatusRepository, EfSystemStatusRepository>();
                services.AddTransient<ITelegramRepository, EfTelegramRepository>();
                services.TryAddCustomerConfiguration();
                services.AddSingleton<ObservableConcurrentQueue<PrismatTelegram>>();
                services.AddSingleton<TelegramQueuePersistenceService>();

                // Housekeeping
                services.AddTransient<IHousekeepingConfigurationRepository, EfHousekeepingConfigurationRepository>();
                services.AddTransient<IHousekeepingAction, HostOrderHousekeepingAction>();
                services.AddTransient<HousekeepingJob>();
                services.AddHostedService<Housekeeping>();

                // Filters
                services.AddSingleton<ISemaphoreProvider, SemaphoreProvider>();
                services.AddSingleton<ISignificanceChecker<ActionGroupCompleted>, ActionGroupEventSignificanceChecker<ActionGroupCompleted>>();
                services.AddSingleton<UnTimedInMemoryFilter<ActionGroupCompleted>>();

                services.AddSingleton<ISignificanceChecker<ActionGroupStarted>, ActionGroupEventSignificanceChecker<ActionGroupStarted>>();
                services.AddSingleton<UnTimedInMemoryFilter<ActionGroupStarted>>();

                services.AddSingleton<ISignificanceChecker<PickActionCompleted>, ActionGroupEventSignificanceChecker<PickActionCompleted>>();
                services.AddSingleton<UnTimedInMemoryFilter<PickActionCompleted>>();

                if (simulateOptions == null)
                {
                    services.TryAddPrismatApi(context.Configuration);
                    services.AddSingleton<IHostCommunicator, TelegramHostCommunicator>();
                    return;
                }

                services.AddSingleton<IHostCommunicator, SimulationHostCommunicator>();
                services.AddSingleton(_ => simulateOptions);

                if (!simulateOptions.NoAplLoopOrders)
                {
                    logger.LogInformation("Simulation configuration: Creating APL loop orders");
                    services.AddHostOrderCreator<LongLoopSimulator>()
                        .AddHostOrderCreator<LongLoopInputSimulator>();
                }
                if (!simulateOptions.NoAklLoopOrders)
                {
                    logger.LogInformation("Simulation configuration: Creating AKL loop orders");
                    services.AddHostOrderCreator<ShortLoopSimulator>()
                        .AddHostOrderCreator<InputLoopSimulator>();
                }
            });
            return hostBuilder;
        }
    }

    /// <summary>
    /// Extension block for <see cref="IServiceCollection"/>.
    /// </summary>
    extension<T>(IServiceCollection services) where T : class, IHostOrderCreator
    {
        private IServiceCollection AddHostOrderCreator()
        {
            services.AddHostedService<PollingBackgroundService<T>>();
            services.AddSingleton<T>();
            services.AddSingleton<IHostOrderCreator, T>(sp => sp.GetRequiredService<T>());

            return services;
        }
    }
}