using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.CustomerConfiguration;
using Gebhardt.Acc.PrismatCommunication.Services.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
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
    private static readonly Lazy<CustomerConfiguration> _config = new(() =>
    {
        var config = new CustomerConfiguration();
        string configFile = GetConfigFile();
        Console.WriteLine($"Using customer configuration from {configFile}.");
        config.UpdateFromFile(configFile);
        return config;
    });

    /// <summary>
    /// Extension block for <see cref="IServiceCollection"/>.
    /// </summary>
    extension(IServiceCollection serviceCollection)
    {
        /// <summary>
        /// Adds services required for the prismat api.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IServiceCollection TryAddPrismatApi(IConfiguration configuration)
        {
            serviceCollection.AddHostedService<PrismatCommunicationManager>();
            serviceCollection.AddCommunication(configuration);
            serviceCollection.AddTelegramProcessors();
            return serviceCollection;
        }

        /// <summary>
        /// Adds communication services required for the prismat api.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        // ReSharper disable once UnusedMethodReturnValue.Local
        private IServiceCollection AddCommunication(IConfiguration configuration)
        {
            var fromPrismatConfig = new TelegramCommunicationConfiguration();
            fromPrismatConfig.LoadFromConfig(configuration.GetRequiredSection(ServiceKeyProvider.FromPrismat));
            serviceCollection.AddKeyedSingleton(ServiceKeyProvider.FromPrismat, (_, _) => fromPrismatConfig);
            var toPrismatConfig = new TelegramCommunicationConfiguration();
            toPrismatConfig.LoadFromConfig(configuration.GetRequiredSection(ServiceKeyProvider.ToPrismat));
            serviceCollection.AddKeyedSingleton(ServiceKeyProvider.ToPrismat, (_, _) => fromPrismatConfig);
            serviceCollection.AddKeyedSingleton<Server>(ServiceKeyProvider.FromPrismat, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Server>>();
                return new Server(logger, fromPrismatConfig);
            });

            serviceCollection.AddKeyedSingleton<Server>(ServiceKeyProvider.ToPrismat, (serviceProvider, _) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Server>>();
                return new Server(logger, toPrismatConfig);
            });

            serviceCollection.AddSingleton(serviceProvider =>
            {
                var toPrismatProcessor = serviceProvider.GetRequiredService<ToPrismatTelegramProcessor>();
                var socket = serviceProvider.GetKeyedService<Server>(ServiceKeyProvider.ToPrismat);
                var logger = serviceProvider.GetRequiredService<ILogger<Sender<PrismatTelegram>>>();
                return new Sender<PrismatTelegram>(socket!, ServiceKeyProvider.ToPrismat, toPrismatProcessor, logger);
            });

            serviceCollection.AddSingleton(serviceProvider =>
            {
                var fromPrismatProcessor = serviceProvider.GetRequiredService<FromPrismatTelegramProcessor>();
                var server = serviceProvider.GetKeyedService<Server>(ServiceKeyProvider.FromPrismat);
                var logger = serviceProvider.GetRequiredService<ILogger<Receiver<PrismatTelegram>>>();
                return new Receiver<PrismatTelegram>(server!, ServiceKeyProvider.FromPrismat, fromPrismatProcessor, logger);
            });

            return serviceCollection;
        }

        /// <summary>
        /// Add telegram processors
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        // ReSharper disable once UnusedMethodReturnValue.Local
        private IServiceCollection AddTelegramProcessors()
        {
            serviceCollection.AddSingleton<ToPrismatTelegramProcessor>();
            serviceCollection.AddSingleton<FromPrismatTelegramProcessor>();
            serviceCollection.AddSingleton<PosStateReqProcessor>();
            serviceCollection.AddSingleton<TordProcessor>();
            serviceCollection.AddSingleton<TordDeleteProcessor>();
            serviceCollection.AddSingleton<LifeProcessor>();
            serviceCollection.AddTransient<TelegramProcessorFactory>();
            return serviceCollection;
        }

        /// <summary>
        /// Adds a <see cref="CustomerConfiguration" /> as singleton to the service collection provided.
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IServiceCollection TryAddCustomerConfiguration()
        {
            serviceCollection.TryAddSingleton(_config.Value);
            return serviceCollection;
        }
    }

    private static string GetConfigFile()
    {
        const string configFile = "customer-configuration.yml";
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