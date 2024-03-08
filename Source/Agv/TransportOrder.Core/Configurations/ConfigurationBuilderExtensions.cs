using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;

/// <summary>
/// Extensions for building the configuration provider
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Injects the default configuration provider
    /// </summary>
    /// <param name="builder">The builder to build the application configuration</param>
    /// <param name="configurationTypeManager"></param>
    /// <param name="index">Index position where the configuration source is added using insert</param>
    /// <returns>The <see cref="IConfigurationBuilder"/> object</returns>
    [UsedImplicitly]
    public static IConfigurationBuilder AddDefaultConfiguration(this IConfigurationBuilder builder, IConfigurationTypeManager configurationTypeManager, int index = 0)
    {
        // DefaultConfigurationProvider should be at index 0 of the provider layer, so that environment variable can overwrite the hardcoded values of a ISettings
        builder.Sources.Insert(index, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
        return builder;
    }
}