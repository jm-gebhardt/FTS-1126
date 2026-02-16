using FluentValidation;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Domain.Services;
using Gebhardt.Acc.OrderTranslator.Domain.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gebhardt.Acc.OrderTranslator.Configuration;

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
        /// Adds a <see cref="StationConfiguration" /> as singleton to the service collection provided.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public IServiceCollection TryAddStationConfiguration()
        {
            serviceCollection.TryAddSingleton(_config.Value);
            return serviceCollection;
        }

        /// <summary>
        /// Register host order validators.
        /// </summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        // ReSharper disable once UnusedMethodReturnValue.Local
        public IServiceCollection RegisterValidators()
        {
            serviceCollection.AddTransient<IHostOrderValidationService, HostOrderValidationService>();
            serviceCollection.AddTransient<IValidator<HostOrder>, HostOrderValidator>();
            serviceCollection.AddTransient<IValidator<Domain.Models.HostOrder.Action>, HostOrderActionValidator>();
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