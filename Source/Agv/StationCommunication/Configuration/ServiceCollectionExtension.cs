using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.ConveyorTelegramApi.Processors;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Gebhardt.Acc.StationCommunication.Services.TelegramApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Gebhardt.Acc.StationCommunication.Domain.Models;

namespace Gebhardt.Acc.StationCommunication.Configuration;

/// <summary>
/// Provides extension method for the service collection
/// </summary>
public static class ServiceCollectionExtension
{
    private static readonly Lazy<StationConfiguration> _config = new(() =>
    {
        var config = new StationConfiguration();
        string configFile = GetConfigFile();
        Console.WriteLine($"Using station configuration from {configFile}.");
        config.ConfigFilePath = configFile;
        return config;
    });

    /// <summary>
    /// Extension block for <see cref="IServiceCollection"/>.
    /// </summary>
    extension(IServiceCollection serviceCollection)
    {
        /// <summary>
        /// Adds services required for the conveyor api.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IServiceCollection AddConveyorApi(IConfiguration configuration)
        {
            serviceCollection.AddWePlcConnection<ToWePlcCommunicationManager>(configuration, ServiceKeyProvider.AplWeStation);
            serviceCollection.AddFromWePlcConnection<FromWePlcCommunicationManager>(configuration, ServiceKeyProvider.FromWeStation);
            serviceCollection.AddAplPlcConnection<AgvCargoStateCommunicationManager>(configuration, ServiceKeyProvider.AgvCargoState);
            return serviceCollection;
        }

        /// <summary>
        /// Adds services required for connecting to the conveyor plc.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="plc">The plc identifier.</param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        // ReSharper disable once UnusedMethodReturnValue.Local
        private IServiceCollection AddWePlcConnection<TCommunicationManager>(IConfiguration configuration, string plc)
            where TCommunicationManager : BackgroundService
        {
            var config = new TelegramCommunicationConfiguration();
            config.LoadFromConfig(configuration.GetRequiredSection(plc));
            serviceCollection.AddKeyedSingleton(plc, (_, _) => config);

            // Connection
            serviceCollection.AddKeyedSingleton<Client>(plc, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Client>>();
                return new Client(logger, config);
            });

            // Queue
            serviceCollection.AddKeyedSingleton<ConcurrentQueue<ConveyorTelegram>>(plc);

            serviceCollection.AddKeyedSingleton<ToWePlcTelegramProcessor>(plc,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ToWePlcTelegramProcessor>>();
                    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
                    var queue = serviceProvider.GetRequiredKeyedService<ConcurrentQueue<ConveyorTelegram>>(plc);
                    return new ToWePlcTelegramProcessor(logger, timeProvider, config, queue);
                });

            serviceCollection.AddKeyedSingleton<Sender<ConveyorTelegram>>(plc,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<Sender<ConveyorTelegram>>>();
                    var socket = serviceProvider.GetKeyedService<Client>(plc);
                    var processor = serviceProvider.GetKeyedService<ToWePlcTelegramProcessor>(plc);
                    return new Sender<ConveyorTelegram>(socket!, plc, processor!, logger);
                });

            serviceCollection.AddHostedService<TCommunicationManager>();
            return serviceCollection;
        }

        /// <summary>
        /// Adds services required for connecting a receiving channel to the conveyor plc.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="plc">The plc identifier.</param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        // ReSharper disable once UnusedMethodReturnValue.Local
        private IServiceCollection AddFromWePlcConnection<TCommunicationManager>(IConfiguration configuration, string plc)
            where TCommunicationManager : BackgroundService
        {
            var config = new TelegramCommunicationConfiguration();
            config.LoadFromConfig(configuration.GetRequiredSection(plc));
            serviceCollection.AddKeyedSingleton(plc, (_, _) => config);
            serviceCollection.AddTransient<TelegramProcessorFactory>();
            serviceCollection.AddSingleton<JamStateProcessor>();
            serviceCollection.AddSingleton<LifeProcessor>();
            serviceCollection.AddSingleton<FromWePlcTelegramProcessor>();
            // Connection
            serviceCollection.AddKeyedSingleton<Client>(plc, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Client>>();
                return new Client(logger, config);
            });

            // Queue
            serviceCollection.AddKeyedSingleton<ConcurrentQueue<ConveyorTelegram>>(plc);

            serviceCollection.AddKeyedSingleton<Receiver<ConveyorTelegram>>(plc,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<Receiver<ConveyorTelegram>>>();
                    var socket = serviceProvider.GetKeyedService<Client>(plc);
                    var processor = serviceProvider.GetRequiredService<FromWePlcTelegramProcessor>();
                    return new Receiver<ConveyorTelegram>(socket!, plc, processor, logger);
                });

            serviceCollection.AddHostedService<TCommunicationManager>();
            return serviceCollection;
        }

        /// <summary>
        /// Adds services required for connecting to the apl plc.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="plc">The plc identifier.</param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        // ReSharper disable once UnusedMethodReturnValue.Local
        private IServiceCollection AddAplPlcConnection<TCommunicationManager>(IConfiguration configuration, string plc) where TCommunicationManager : BackgroundService
        {
            var config = new TelegramCommunicationConfiguration();
            config.LoadFromConfig(configuration.GetRequiredSection(plc));
            serviceCollection.AddKeyedSingleton(plc, (_, _) => config);

            // Connection
            serviceCollection.AddKeyedSingleton<Client>(plc, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Client>>();
                return new Client(logger, config);
            });

            serviceCollection.AddKeyedSingleton<AgvAplCargoStateProcessor>(plc,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<AgvAplCargoStateProcessor>>();
                    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
                    return new AgvAplCargoStateProcessor(logger, timeProvider, config);
                });

            serviceCollection.AddKeyedSingleton<Sender<ConveyorTelegram>>(plc,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<Sender<ConveyorTelegram>>>();
                    var socket = serviceProvider.GetKeyedService<Client>(plc);
                    var processor = serviceProvider.GetKeyedService<AgvAplCargoStateProcessor>(plc);
                    return new Sender<ConveyorTelegram>(socket!, plc, processor!, logger);
                });

            serviceCollection.AddHostedService<TCommunicationManager>();
            return serviceCollection;
        }

        /// <summary>
        /// Adds a <see cref="StationConfiguration" /> as singleton to the service collection provided.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IServiceCollection TryAddStationConfiguration()
        {
            serviceCollection.TryAddSingleton(_config.Value);
            return serviceCollection;
        }
    }

    private static string GetConfigFile()
    {
        const string configFile = "station-configuration.yml";
        const string configDirectory = "Configuration";
        string[] files = [];
        string? current = Path.Join(Directory.GetCurrentDirectory(), configDirectory);

        // Traverse the directory tree upwards to find a config file.
        while (files.Length == 0 && current != null)
        {
            files = Directory.GetFiles(current, configFile);
            current = Directory.GetParent(current)?.FullName ?? null;
        }

        if (files.Length == 0)
        {
            throw new FileNotFoundException($"{configFile} could not be found.");
        }

        return files.First();
    }
}