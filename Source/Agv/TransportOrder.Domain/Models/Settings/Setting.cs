namespace Gebhardt.Acc.TransportOrder.Domain.Models.Settings;

/// <summary>
/// Provides a setting.
/// </summary>
public class Setting
{
    /// <inheritdoc cref="Setting"/>>
    public Setting(Guid id, string name, string value, string valueType, bool isDefault)
    {
        Id = id;
        Name = name;
        Value = value;
        ValueType = valueType;
        IsDefault = isDefault;
    }

    /// <summary>
    /// The identifier of the setting
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// The name of the settings value. 
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The value in string representation.
    /// </summary>
    public string Value { get; set; } 

    /// <summary>
    /// The actual type of the settings value.
    /// </summary>
    public string ValueType { get; init; } 
    
    /// <summary>
    /// True if the settings value is a default.
    /// </summary>
    public bool IsDefault { get; init; } 
}