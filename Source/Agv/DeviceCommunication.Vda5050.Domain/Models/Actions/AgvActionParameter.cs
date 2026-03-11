using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Actions;

/// <summary>
/// Definition of a parameter accepted by an AGV action (as declared in the fact sheet).
/// </summary>
[PublicAPI]
public record AgvActionParameter
{
    /// <summary>
    /// Key string for the parameter.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Data type of the parameter value.
    /// </summary>
    public ValueDataType ValueDataType { get; set; }

    /// <summary>
    /// Free text description of the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// True: this parameter is optional.
    /// </summary>
    public bool? IsOptional { get; set; }
}

/// <summary>
/// Data type of action parameter value.
/// </summary>
[PublicAPI]
public enum ValueDataType
{
    /// <summary/>
    Bool,

    /// <summary/>
    Number,

    /// <summary/>
    Integer,

    /// <summary/>
    Float,

    /// <summary/>
    String,

    /// <summary/>
    Object,

    /// <summary/>
    Array
}