using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
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
    private const string ConfigurationSection = "CONVEYOR";

    /// <summary>
    /// Adds services required for the conveyor api.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The configuration</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection AddConveyorApi(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var config = new TelegramCommunicationConfiguration();
        config.LoadFromConfig(configuration.GetRequiredSection(ConfigurationSection));
        serviceCollection.AddSingleton(config);
        serviceCollection.AddSingleton<ToConveyorTelegramProcessor>();
        serviceCollection.AddSingleton<Client>();
        serviceCollection.AddSingleton<Sender<ConveyorTelegram>>(serviceProvider =>
        {
            var processor = serviceProvider.GetRequiredService<ToConveyorTelegramProcessor>();
            var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<ConveyorTelegram>>>();
            var socket = serviceProvider.GetRequiredService<Client>();
            return new Sender<ConveyorTelegram>(socket, "conveyorSenders", processor, logger);
        });
        serviceCollection.AddHostedService<ConveyorCommunicationManager>();
        return serviceCollection;
    }
}