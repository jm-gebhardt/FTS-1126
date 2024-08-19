using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// A configuration parameter
/// </summary>
[PublicAPI]
public record ConfigurationParameter
{
    /// <inheritdoc cref="ConfigurationParameter"/>>
    public ConfigurationParameter(string name, string description, string value, string validation)
    {
        Name = name;
        Description = description;
        Value = value;
        Validation = validation;
    }

    /// <summary>
    /// The parameter name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The parameter description. 
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// The parameter value.
    /// </summary>
    public string Value { get; set; } 

    /// <summary>
    /// The parameter validation regex.
    /// </summary>
    public string Validation { get; init; } 
}
