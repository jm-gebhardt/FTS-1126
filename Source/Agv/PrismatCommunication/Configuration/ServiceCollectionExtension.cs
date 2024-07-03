using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Services.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.TelegramApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Configuration;

/// <summary>
/// Provides extension method for the service collection
/// </summary>
public static class ServiceCollectionExtension
{
    private static readonly Lazy<KaeserGraphConfiguration> _config = new(() =>
    {
        var config = new KaeserGraphConfiguration(new InMemoryTriggerPointMapping());
        string configFile = GetConfigFile();
        Console.WriteLine($"Using Kaeser graph configuration from {configFile}.");
        config.UpdateFromFile(configFile);
        return config;
    });

    /// <summary>
    /// Adds services required for the prismat api.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The configuration</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection TryAddPrismatApi(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddHostedService<PrismatCommunicationManager>();
        serviceCollection.AddCommunication(configuration);
        serviceCollection.AddTelegramProcessors();

        return serviceCollection;
    }

    /// <summary>
    /// Adds communication services required for the prismat api.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="configuration">The configuration</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddCommunication(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var fromPrismatConfig = new TelegramCommunicationConfiguration();
        fromPrismatConfig.LoadFromConfig(configuration.GetRequiredSection(ServiceKeyProvider.FromPrismat));
        serviceCollection.AddKeyedSingleton(ServiceKeyProvider.FromPrismat, (_, _) => fromPrismatConfig);
        var toPrismatConfig = new TelegramCommunicationConfiguration();
        toPrismatConfig.LoadFromConfig(configuration.GetRequiredSection(ServiceKeyProvider.ToPrismat));
        serviceCollection.AddKeyedSingleton(ServiceKeyProvider.ToPrismat, (_, _) => fromPrismatConfig);
        serviceCollection.AddKeyedSingleton<Server>(ServiceKeyProvider.FromPrismat, (serviceProvider, _) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SocketBase>>();
            return new Server(logger, fromPrismatConfig);
        });

        serviceCollection.AddKeyedSingleton<Server>(ServiceKeyProvider.ToPrismat, (serviceProvider, _) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SocketBase>>();
            return new Server(logger, toPrismatConfig);
        });

        serviceCollection.AddSingleton(serviceProvider =>
        {
            var toPrismatProcessor = serviceProvider.GetRequiredService<ToPrismatTelegramProcessor>();
            var socket = serviceProvider.GetKeyedService<Server>(ServiceKeyProvider.ToPrismat);
            var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<PrismatTelegram>>>();
            return new Sender<PrismatTelegram>(socket!, ServiceKeyProvider.ToPrismat, toPrismatProcessor, logger);
        });

        serviceCollection.AddSingleton(serviceProvider =>
        {
            var fromPrismatProcessor = serviceProvider.GetRequiredService<FromPrismatTelegramProcessor>();
            var server = serviceProvider.GetKeyedService<Server>(ServiceKeyProvider.FromPrismat);
            var logger = serviceProvider.GetRequiredService<ILogger<SocketCommunicator<PrismatTelegram>>>();
            return new Receiver<PrismatTelegram>(server!, ServiceKeyProvider.FromPrismat, fromPrismatProcessor, logger);
        });

        return serviceCollection;
    }

    /// <summary>
    /// Add telegram processors
    /// </summary>
    /// <param name="serviceCollection">The service collection</param>
    // ReSharper disable once UnusedMethodReturnValue.Global
    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddTelegramProcessors(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ToPrismatTelegramProcessor>();
        serviceCollection.AddSingleton<FromPrismatTelegramProcessor>();
        serviceCollection.AddSingleton<TordProcessor>();
        serviceCollection.AddSingleton<LifeProcessor>();
        serviceCollection.AddTransient<TelegramProcessorFactory>();
        return serviceCollection;
    }

    /// <summary>
    /// Adds a <see cref="KaeserGraphConfiguration" /> as singleton to the service collection provided.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="isSimulationMode">Whether this function is called from the simulation mode.</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection TryAddKaeserGraphConfiguration(this IServiceCollection serviceCollection, bool isSimulationMode)
    {
        if (isSimulationMode)
        {
            _config.Value.UpdateTriggerPointNodeId(_config.Value.DecisionPointName, "WP-021");
            _config.Value.UpdateTriggerPointNodeId(_config.Value.LoadTriggerPointName, "WP-018");
        }
        serviceCollection.TryAddSingleton(_config.Value);
        return serviceCollection;
    }

    private static string GetConfigFile()
    {
        const string configFile = "graph-configuration.yml";
        const string configDirectory = "Configuration";
        string[] files = Array.Empty<string>();
        string? current = Path.Join(Directory.GetCurrentDirectory(), configDirectory);

        // Traverse the directory tree upwards to find a config file.
        while (!files.Any() && current != null)
        {
            files = Directory.GetFiles(current, configFile);
            current = Directory.GetParent(current)?.FullName ?? null;
        }

        if (!files.Any())
        {
            throw new FileNotFoundException($"{configFile} could not be found.");
        }

        return files.First();
    }
}