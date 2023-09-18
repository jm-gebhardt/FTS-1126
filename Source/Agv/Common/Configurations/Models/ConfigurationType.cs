using System.Reflection;
using System.Text.Json;
using Gebhardt.StoreWare.Agv.Common.Configurations.Attributes;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.Models;

/// <summary>
/// The model for the type of configuration holding all properties
/// </summary>
public class ConfigurationType
{
    /// <summary>
    /// <inheritdoc cref="ConfigurationType"/>
    /// </summary>
    public ConfigurationType(Type settingType, bool autoSeedToDatabase)
    {
        Name = settingType.Name;
        Type = settingType;
        AutoSeedToDatabase = autoSeedToDatabase;
        DefaultInstance = Activator.CreateInstance(settingType)!;
        SectionName = GetSectionName(settingType);

        Properties = settingType.GetProperties().Select(p => new ConfigurationTypeProperty($"{SectionName}:{p.Name}", GetValue(p), ShouldAutoSeedToDatabase(p)));
    }

    private static bool ShouldAutoSeedToDatabase(PropertyInfo propertyInfo)
    {
        return (propertyInfo.GetCustomAttribute(typeof(AutoSeedToDatabase)) as AutoSeedToDatabase)?.AutoSeedToDatabaseValue ?? true;
    }

    /// <summary>
    /// The name of the type
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The section name of the configuration
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// The type used for the configuration
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Should it be automatically seeded to the default database?
    /// </summary>
    public bool AutoSeedToDatabase { get; }

    /// <summary>
    ///  The properties of the configuration
    /// </summary>
    public IEnumerable<ConfigurationTypeProperty> Properties { get; }

    /// <summary>
    /// An object of the type constructed by using the default ctor with it's default values
    /// </summary>
    [UsedImplicitly]
    public object DefaultInstance { get; }

    private string GetSectionName(Type settingType)
    {
        if (settingType.GetProperty(nameof(SectionName))?.GetValue(DefaultInstance, null) is not string value)
        {
            throw new ArgumentException($"{nameof(SectionName)} not defined!");
        }

        return value;
    }

    /// <summary>
    /// Gets the property value by determining if its primitive or reference type.
    /// If reference type, the value is serialized.
    /// </summary>
    private string GetValue(PropertyInfo propertyInfo)
    {
        var propertyType = propertyInfo.PropertyType;
        object? propertyValue = propertyInfo.GetValue(DefaultInstance);
        if (propertyType.IsPrimitive || propertyType.IsEnum || propertyType == typeof(string))
        {
            return propertyValue?.ToString()!;
        }
        else
        {
            return JsonSerializer.Serialize(propertyValue);
        }
    }
}