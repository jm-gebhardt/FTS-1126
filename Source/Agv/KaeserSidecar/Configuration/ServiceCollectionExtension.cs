using System.Collections.Concurrent;
using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Gebhardt.Acc.KaeserSidecar.Services.TelegramApi;
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
    }
}