using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Filter.Enums;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.Api.Dtos;

/// <summary>
/// Provides a dto for a collection aggregate filter.
/// </summary>
[PublicAPI]
public record CollectionFilterDto : FilterDto
{
    /// <summary>
    /// The aggregation to perform.
    /// </summary>
    public CollectionAggregateVariant Aggregation { get; } = CollectionAggregateVariant.All;

    /// <summary>
    /// The child filter.
    /// </summary>
    public FilterDto Child { get; }

    /// <summary>
    /// The fully qualified name of the collection from the root of the read model. Nesting levels separated by &quot;.&quot;
    /// </summary>
    public string Name { get; } = string.Empty;

    /// <summary>
    /// Constructor
    /// </summary>
    [JsonConstructor]
    public CollectionFilterDto(FilterDto child, string name, CollectionAggregateVariant aggregation)
    {
        Child = child;
        Aggregation = aggregation;
        Name = name;
    }
}