namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// A configuration parameter
/// </summary>
public record ConfigurationParameter
{
    /// <inheritdoc cref="ConfigurationParameter"/>>
    public ConfigurationParameter(string name, string value, string validation)
    {
        Name = name;
        Value = value;
        Validation = validation;
    }

    /// <summary>
    /// The parameter name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The parameter value.
    /// </summary>
    public string Value { get; set; } 

    /// <summary>
    /// The parameter validation regex.
    /// </summary>
    public string Validation { get; init; } 
}
