using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Gebhardt.Acc.KaeserSidecar.Services.TelegramApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        // Configuration
        var aplWeStationConfig = new TelegramCommunicationConfiguration();
        aplWeStationConfig.LoadFromConfig(configuration.GetRequiredSection(ServiceKeyProvider.AplWeStation));
        serviceCollection.AddKeyedSingleton(ServiceKeyProvider.AplWeStation, (_, _) => aplWeStationConfig);

        var agvCargoStateConfig = new TelegramCommunicationConfiguration();
        agvCargoStateConfig.LoadFromConfig(configuration.GetRequiredSection(ServiceKeyProvider.AgvCargoState));
        serviceCollection.AddKeyedSingleton(ServiceKeyProvider.AgvCargoState, (_, _) => agvCargoStateConfig);

        // Clients
        serviceCollection.AddKeyedSingleton<Client>(ServiceKeyProvider.AplWeStation, (serviceProvider, _) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SocketBase>>();
            return new Client(logger, aplWeStationConfig);
        });

        serviceCollection.AddKeyedSingleton<Client>(ServiceKeyProvider.AgvCargoState, (serviceProvider, _) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SocketBase>>();
            return new Client(logger, agvCargoStateConfig);
        });

        // Telegram processors
        serviceCollection.AddKeyedSingleton<ToConveyorTelegramProcessor>(ServiceKeyProvider.AplWeStation,
            (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ToConveyorTelegramProcessor>>();
                var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
                return new ToConveyorTelegramProcessor(logger, timeProvider, aplWeStationConfig);
            });

        serviceCollection.AddKeyedSingleton<ToConveyorTelegramProcessor>(ServiceKeyProvider.AgvCargoState,
            (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ToConveyorTelegramProcessor>>();
                var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
                return new ToConveyorTelegramProcessor(logger, timeProvider, agvCargoStateConfig);
            });

        // Sender
        serviceCollection.AddKeyedSingleton<Sender<ConveyorTelegram>>(ServiceKeyProvider.AplWeStation,
            (serviceProvider,_ ) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<ConveyorTelegram>>>();
            var socket = serviceProvider.GetKeyedService<Client>(ServiceKeyProvider.AplWeStation);
            var processor = serviceProvider.GetKeyedService<ToConveyorTelegramProcessor>(ServiceKeyProvider.AplWeStation);
            return new Sender<ConveyorTelegram>(socket!, ServiceKeyProvider.AplWeStation, processor!, logger);
        });

        serviceCollection.AddKeyedSingleton<Sender<ConveyorTelegram>>(ServiceKeyProvider.AgvCargoState,
            (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<ConveyorTelegram>>>();
                var socket = serviceProvider.GetKeyedService<Client>(ServiceKeyProvider.AgvCargoState);
                var processor = serviceProvider.GetKeyedService<ToConveyorTelegramProcessor>(ServiceKeyProvider.AgvCargoState);
                return new Sender<ConveyorTelegram>(socket!, ServiceKeyProvider.AgvCargoState, processor!, logger);
            });

        // Communication managers
        serviceCollection.AddHostedService<AplWeStationCommunicationManager>();
        serviceCollection.AddHostedService<AgvCargoStateCommunicationManager>();
        return serviceCollection;
    }
}