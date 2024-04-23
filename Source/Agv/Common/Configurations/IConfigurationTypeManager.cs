using Gebhardt.Acc.Common.Configurations.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Configurations;

/// <summary>
/// Holds all configuration types of the initialized assembly(/ies)
/// </summary>
public interface IConfigurationTypeManager
{
    /// <summary>
    /// All found configuration types
    /// </summary>
    IList<ConfigurationType> ConfigurationTypes { get; }

    /// <summary>
    /// Returns the default instance of the setting
    /// </summary>
    /// <exception cref="ArgumentException">If not found</exception>
    [UsedImplicitly]
    public TSettings GetDefaultInstance<TSettings>() where TSettings : class, ISettings<TSettings>;

    /// <summary>
    /// Returns the SectionName of the setting
    /// </summary>
    /// <exception cref="ArgumentException">If setting was not found</exception>
    [UsedImplicitly]
    public string GetSectionName<TSettings>() where TSettings : class, ISettings<TSettings>;

    /// <summary>
    /// Returns the ConfigurationType based on the given name of the type
    /// </summary>
    [PublicAPI]
    ConfigurationType? GetConfigurationTypeByTypeName(string configurationTypeName);
}