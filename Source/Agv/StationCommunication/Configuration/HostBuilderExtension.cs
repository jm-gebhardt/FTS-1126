using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.ConveyorTelegramApi.Services;
using Gebhardt.Acc.StationCommunication.CmdOptions;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Domain.Services;
using Gebhardt.Acc.StationCommunication.Persistence.Ef.DataAccess;
using Gebhardt.Acc.StationCommunication.Persistence.Ef.Repositories;
using Gebhardt.Acc.StationCommunication.Services;
using Gebhardt.Acc.StationCommunication.Services.BackgroundServices;
using Gebhardt.Acc.StationCommunication.SimulationMode;
using Gebhardt.Core.EntityFrameworkCore;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Configuration;

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
        public IHostBuilder AddMassTransit(ILogger initialLogger)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                try
                {
                    services.AddSingleton(TimeProvider.System);
                    //Configure masstransit
                    services.ConfigureServicesMassTransit(context.Configuration, x => { x.AddBusConsumers(); });
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
        /// Add service specific configurations
        /// </summary>
        public IHostBuilder AddStationCommandHandler(SimulateOptions? simulateOptions)
        {
            ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddHostedService<InitializationProvider>();
                services.AddTransient<IJamStateRepository, EfJamStateRepository>();
                services.AddTransient<IStationRepository, EfStationRepository>();
                services.AddSingleton<IOrderRepository, EfOrderRepository>();
                services.TryAddStationConfiguration();
                services.AddSingleton<IConcurrentDictionaryProvider<string, bool>, ConcurrentDictionaryProvider<string, bool>>();
                services.AddSingleton<IConcurrentDictionaryProvider<Guid, bool>, ConcurrentDictionaryProvider<Guid, bool>>();
                services.AddSingleton<IPlcConnectionRepository, PlcConnectionRepository>();
                services.AddSingleton<ActiveChannelKeyProvider>();
                services.AddDbContextFactory<StationCommunicationDbContext>(builder =>
                {
                    string? connectionString = context.Configuration.GetConnectionString(DbProviders.SqlServer)
                                               ??context.Configuration.GetConnectionString(StationCommunicationDbContext.ConfigSectionName);
                    builder.UseSqlServer(connectionString, m =>
                    {
                        m.MigrationsHistoryTable(IDbContext<StationCommunicationDbContext>.MigrationTableName);
                        m.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                    builder.AddInterceptors(new EfCommandMetricsInterceptor());
                });
                if (simulateOptions != null)
                {
                    services.AddSingleton<IConveyorCommunicator, SimulationConveyorCommunicator>();
                    services.AddHostedService<SimulationStationOnlinePublisher>();
                }
                else
                {
                    services.AddConveyorApi(context.Configuration);
                    services.AddSingleton<IConveyorCommunicator, TelegramConveyorCommunicator>();
                }
            });
            return hostBuilder;
        }
    }
}