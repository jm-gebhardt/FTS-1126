using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

/// <summary>
/// A dto for a configuration parameter
/// </summary>
[PublicAPI]
public record ConfigurationParameterDto
{
    /// <inheritdoc cref="ConfigurationParameterDto"/>>
    public ConfigurationParameterDto(string name, string value, string validation)
    {
        Name = name;
        Value = value;
        Validation = validation;
    }

    /// <inheritdoc cref="ConfigurationParameterDto"/>>
    public ConfigurationParameterDto()
    {
        Name = string.Empty;
        Value = string.Empty;
        Validation = string.Empty;
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