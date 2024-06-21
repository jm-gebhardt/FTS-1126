using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// Adds a <see cref="KaeserGraphConfiguration" /> as singleton to the service collection provided.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection TryAddKaeserGraphConfiguration(this IServiceCollection serviceCollection)
    {
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