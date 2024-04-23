using Gebhardt.Acc.Common.Configurations.ConfigurationProviders;
using Microsoft.Extensions.Configuration;

namespace Gebhardt.Acc.Common.Configurations.SettingsProviders;

/// <summary>
/// Config Entity Configuration Source
/// </summary>
public class DefaultConfigurationSource : IConfigurationSource
{
    private readonly IConfigurationTypeManager _configurationTypeManager;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="configurationTypeManager"></param>
    public DefaultConfigurationSource(IConfigurationTypeManager configurationTypeManager)
    {
        _configurationTypeManager = configurationTypeManager;
    }

    /// <inheritdoc/>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DefaultConfigurationProvider(_configurationTypeManager);
    }
}