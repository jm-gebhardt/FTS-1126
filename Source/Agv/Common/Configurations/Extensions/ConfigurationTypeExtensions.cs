using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;

/// <summary>
/// Extension methods for ConfigurationTypes
/// </summary>
public static class ConfigurationTypeExtensions
{
    /// <summary>
    /// Registers <see cref="ConfigurationTypeManager"/> as singleton and initializes it
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <returns><see cref="IServiceCollection"/></returns>
    [UsedImplicitly]
    public static IServiceCollection AddConfigurationTypeManager(this IServiceCollection services)
    {
        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
        services.AddSingleton<IConfigurationTypeManager, ConfigurationTypeManager>(_ => ConfigurationTypeManager.Instance);

        return services;
    }
}