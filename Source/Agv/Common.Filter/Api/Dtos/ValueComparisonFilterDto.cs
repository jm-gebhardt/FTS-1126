using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Filter.Enums;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.Api.Dtos;

/// <summary>
/// Represents a filter leaf that compares a value with the property behind the name.
/// </summary>
[PublicAPI]
public record ValueComparisonFilterDto : FilterBaseDto
{
    /// <summary>
    /// The property name as reachable in the hierarchy from the root of the comparison.
    /// </summary>
    public string Name { get; } = string.Empty;

    /// <summary>
    /// How the property will be compared.
    /// </summary>
    public ComparisonVariant Variant { get; } = ComparisonVariant.Equals;

    /// <summary>
    /// String representation of what the property will be compared with.
    /// Will be attempted to be converted to the type that the property has on the read model.
    /// </summary>
    public string Content { get; } = string.Empty;

    /// <summary>
    /// Constructor.
    /// </summary>
    [JsonConstructor]
    public ValueComparisonFilterDto(string name, ComparisonVariant variant, string content)
    {
        Name = name;
        Variant = variant;
        Content = content;
    }
}