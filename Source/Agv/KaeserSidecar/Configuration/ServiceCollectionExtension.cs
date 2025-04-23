using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.TelegramApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Configuration;

/// <summary>
/// Provides extension method for the service collection
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Adds services required for the conveyor api.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The configuration</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection AddConveyorApi(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddWePlcConnection<AgvWeStationTelegram, AplWeStationCommunicationManager>(configuration, ServiceKeyProvider.AplWeStation);
        serviceCollection.AddAplPlcConnection<AgvCargoStateCommunicationManager>(configuration, ServiceKeyProvider.AgvCargoState);
        return serviceCollection;
    }

    /// <summary>
    /// Adds services required for connecting to the conveyor plc.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="plc">The plc identifier.</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddWePlcConnection<TTelegramType, TCommunicationManager>(this IServiceCollection serviceCollection, IConfiguration configuration, string plc)
    where TTelegramType: Telegram
    where TCommunicationManager: BackgroundService
    {
        var config = new TelegramCommunicationConfiguration();
        config.LoadFromConfig(configuration.GetRequiredSection(plc));
        serviceCollection.AddKeyedSingleton(plc, (_, _) => config);

        // Connection
        serviceCollection.AddKeyedSingleton<Client>(plc, (serviceProvider, _) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SocketBase>>();
            return new Client(logger, config);
        });

        serviceCollection.AddKeyedSingleton<AgvWeStationTelegramProcessor>(plc,
            (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AgvWeStationTelegramProcessor>>();
                var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
                var queue = serviceProvider.GetRequiredKeyedService<ObservableConcurrentQueue<ConveyorTelegram>>(plc);
                return new AgvWeStationTelegramProcessor(logger, timeProvider, config, queue);
            });

        serviceCollection.AddKeyedSingleton<Sender<ConveyorTelegram>>(plc,
            (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<ConveyorTelegram>>>();
                var socket = serviceProvider.GetKeyedService<Client>(plc);
                var processor = serviceProvider.GetKeyedService<AgvWeStationTelegramProcessor>(plc);
                return new Sender<ConveyorTelegram>(socket!, plc, processor!, logger);
            });

        serviceCollection.AddHostedService<TCommunicationManager>();

        // Telegram persistence
        serviceCollection.AddKeyedTransient<ITelegramRepository>(plc, (serviceProvider, _) =>
        {
            var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<KaeserSidecarDbContext>>();
            return new EfTelegramRepository<TTelegramType>(contextFactory);
        });

        serviceCollection.AddKeyedSingleton<ObservableConcurrentQueue<ConveyorTelegram>>(plc);

        serviceCollection.AddKeyedSingleton<TelegramQueuePersistenceService>(plc, (serviceProvider, _) =>
        {
            var queue = serviceProvider.GetRequiredKeyedService<ObservableConcurrentQueue<ConveyorTelegram>>(plc);
            var repo = serviceProvider.GetRequiredKeyedService<ITelegramRepository>(plc);
            var logger = serviceProvider.GetRequiredService<ILogger<TelegramQueuePersistenceService>>();
            return new TelegramQueuePersistenceService(queue, repo, logger);
        });

        return serviceCollection;
    }

    /// <summary>
    /// Adds services required for connecting to the apl plc.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="plc">The plc identifier.</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddAplPlcConnection<TCommunicationManager>(this IServiceCollection serviceCollection, IConfiguration configuration, string plc) where TCommunicationManager : BackgroundService
    {
        var config = new TelegramCommunicationConfiguration();
        config.LoadFromConfig(configuration.GetRequiredSection(plc));
        serviceCollection.AddKeyedSingleton(plc, (_, _) => config);

        // Connection
        serviceCollection.AddKeyedSingleton<Client>(plc, (serviceProvider, _) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SocketBase>>();
            return new Client(logger, config);
        });

        serviceCollection.AddKeyedSingleton<AgvAplCargoStateProcessor>(plc,
            (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AgvAplCargoStateProcessor>>();
                var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
                return new AgvAplCargoStateProcessor(logger, timeProvider, config);
            });

        serviceCollection.AddKeyedSingleton<Sender<ConveyorTelegram>>(plc,
            (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<ConveyorTelegram>>>();
                var socket = serviceProvider.GetKeyedService<Client>(plc);
                var processor = serviceProvider.GetKeyedService<AgvAplCargoStateProcessor>(plc);
                return new Sender<ConveyorTelegram>(socket!, plc, processor!, logger);
            });

        serviceCollection.AddHostedService<TCommunicationManager>();
        return serviceCollection;
    }
}