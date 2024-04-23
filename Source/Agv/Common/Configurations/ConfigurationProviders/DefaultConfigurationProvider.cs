using Microsoft.Extensions.Configuration;

namespace Gebhardt.Acc.Common.Configurations.ConfigurationProviders;

/// <summary>
/// Provides the default value of all properties of configuration providers
/// </summary>
public class DefaultConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationTypeManager _configurationTypeManager;

    /// <summary>
    /// <inheritdoc cref="DefaultConfigurationProvider" />
    /// </summary>
    /// <param name="configurationTypeManager"></param>
    public DefaultConfigurationProvider(IConfigurationTypeManager configurationTypeManager)
    {
        _configurationTypeManager = configurationTypeManager;
    }

    /// <inheritdoc/>
    public override void Load()
    {
        Data = new Dictionary<string, string>()!;

        foreach (var configurationTypeProperty in _configurationTypeManager.ConfigurationTypes.SelectMany(c => c.Properties))
        {
            Data.Add(configurationTypeProperty.Key, configurationTypeProperty.Value);
        }
    }
}