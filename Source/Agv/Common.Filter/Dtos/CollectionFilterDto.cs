using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Filter.Enums;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.Dtos;

/// <summary>
/// Represents a collection aggregate filter. Must have exactly one leaf or child, but not both.
/// </summary>
[PublicAPI]
public record CollectionFilterDto : FilterBaseDto
{
    /// <summary>
    /// The aggregation to perform.
    /// </summary>
    public CollectionAggregateVariant Aggregation { get; } = CollectionAggregateVariant.All;

    /// <summary>
    /// The leaf. Mutually exclusive with child.
    /// </summary>
    public FilterLeafDto? Leaf { get; } 
    
    /// <summary>
    /// The child filter. Mutually exclusive with leaf.
    /// </summary>
    public FilterBaseDto? Child { get; }

    /// <summary>
    /// The fully qualified name of the collection from the root of the read model.
    /// </summary>
    public string Name { get; } = string.Empty;

    /// <summary>
    /// Constructor
    /// </summary>
    public CollectionFilterDto(FilterLeafDto leaf, string name, CollectionAggregateVariant aggregation) 
    {
        Leaf = leaf;
        Aggregation = aggregation;
        Name = name;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public CollectionFilterDto(FilterBaseDto child, string name, CollectionAggregateVariant aggregation)
    {
        Child = child;
        Aggregation = aggregation;
        Name = name;
    }

    /// <summary>
    /// Json constructor.
    /// </summary>
    [JsonConstructor]
    public CollectionFilterDto(FilterBaseDto? child, FilterLeafDto? leaf, string name, CollectionAggregateVariant aggregation)
    {
        Child = child;
        Leaf = leaf;
        Name = name;
        Aggregation = aggregation;
    }
}