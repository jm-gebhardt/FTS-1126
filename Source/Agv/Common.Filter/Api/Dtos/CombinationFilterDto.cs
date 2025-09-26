using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Filter.Enums;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.Api.Dtos;

/// <summary>
/// Provides a dto for a combination filter. All children of this filter will be semantically combined with the combination variant.
/// </summary>
[PublicAPI]
public record CombinationFilterDto : FilterDto
{
    /// <summary>
    /// The children attached to this filter.
    /// </summary>
    public ICollection<FilterDto> Children { get; } 

    /// <summary>
    /// The combination variant that the leaves and child nodes will be combined with.
    /// </summary>
    public LogicalCombinationVariant Combination { get; } = LogicalCombinationVariant.And;

    /// <summary>
    /// Constructor.
    /// </summary>
    [JsonConstructor]
    public CombinationFilterDto(ICollection<FilterDto> children, LogicalCombinationVariant combination)
    {
        Children = children;
        Combination = combination;
    }
}