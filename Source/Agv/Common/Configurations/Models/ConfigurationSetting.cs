namespace Gebhardt.StoreWare.Agv.Common.Configurations.Models;

/// <summary>
/// The entity for configuration setting
/// </summary>
public class ConfigurationSetting
{
    /// <summary>
    /// The unique key of the configuration setting
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// The value of the configuration setting
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// The type name of the setting used for the configuration
    /// </summary>
    public string ConfigurationType { get; set; } = null!;

}