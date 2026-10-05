using System.Reflection;
using Gebhardt.Acc.Common.Configurations.Attributes;
using Gebhardt.Acc.Common.Configurations.Models;

namespace Gebhardt.Acc.Common.Configurations;

/// <summary>
/// Manages the configuration types from different assemblies
/// </summary>
public sealed class ConfigurationTypeManager : IConfigurationTypeManager
{
    private static readonly Lazy<ConfigurationTypeManager> _lazy = new(() => new ConfigurationTypeManager());
    private IList<ConfigurationType>? _configurationTypes;

    private static IEnumerable<ConfigurationType> GetConfigurationTypes(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var configurationTypes = assembly.GetTypes()
                .Where(t => t.IsAssignableTo(typeof(ISettings)) && t is { IsInterface: false, IsGenericParameter: false, IsGenericType: false })
                .Select(t => new ConfigurationType(t, ShouldAutoSeedToDatabase(t)));

            foreach (var configurationType in configurationTypes)
            {
                yield return configurationType;
            }
        }
    }

    private static bool ShouldAutoSeedToDatabase(Type type)
    {
        return (type.GetCustomAttribute(typeof(AutoSeedToDatabase)) as AutoSeedToDatabase)?.AutoSeedToDatabaseValue ?? true;
    }

    /// <summary>
    /// Singleton instance of <see cref="ConfigurationTypeManager"/>.
    /// </summary>
    public static ConfigurationTypeManager Instance => _lazy.Value;

    /// <summary>
    /// Initializes the <see cref="ConfigurationTypes"/> property
    /// </summary>
    public IList<ConfigurationType> InitializeConfigurationTypes()
    {
        var configurationTypes = GetConfigurationTypes(AppDomain.CurrentDomain.GetAssemblies()).ToList();
        _configurationTypes = configurationTypes;
        return configurationTypes;
    }

    /// <inheritdoc />
    public IList<ConfigurationType> ConfigurationTypes => _configurationTypes ??= InitializeConfigurationTypes();

    /// <inheritdoc/>
    public TSettings GetDefaultInstance<TSettings>() where TSettings : class, ISettings<TSettings>
    {
        var configurationType = ConfigurationTypes.SingleOrDefault(c => c.Type == typeof(TSettings));

        if (configurationType == null)
        {
            throw new ArgumentException($"{typeof(TSettings).Name} not found!");
        }

        return (TSettings)configurationType.DefaultInstance;
    }

    /// <inheritdoc/>
    public ConfigurationType? GetConfigurationTypeByTypeName(string configurationTypeName)
    {
        return ConfigurationTypes.SingleOrDefault(c => c.Name == configurationTypeName);
    }

    /// <inheritdoc/>
    public string GetSectionName<TSettings>() where TSettings : class, ISettings<TSettings>
    {
        return GetDefaultInstance<TSettings>().SectionName;
    }
}