using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Filter.Enums;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.Api.Dtos;

/// <summary>
/// Provides a dto for a direct value comparison to a property.
/// </summary>
[PublicAPI]
public record ValueComparisonFilterDto : FilterDto
{
    /// <summary>
    /// The property name as reachable in the hierarchy from the root of the comparison. Different nesting levels should be accessed with "." i.e. "A.B" <br/>
    /// The correct name can be found in the schema extensions for the property on the read model.
    /// </summary>
    public string Name { get; } = string.Empty;

    /// <summary>
    /// How the property will be compared.
    /// </summary>
    public ComparisonVariant Variant { get; } = ComparisonVariant.Equals;

    /// <summary>
    /// String representation of what the property will be compared with. <br/>
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