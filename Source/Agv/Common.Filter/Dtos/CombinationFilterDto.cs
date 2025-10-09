using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Filter.Enums;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.Dtos;

/// <summary>
/// Data transfer object of a direct filter i.e. one where the proper
/// </summary>
[PublicAPI]
public record CombinationFilterDto : FilterBaseDto
{
    /// <summary>
    /// The children attached to this filter.
    /// </summary>
    public ICollection<FilterBaseDto> ChildNodes { get; } 

    /// <summary>
    /// The leaves attached to this filter.
    /// </summary>
    public ICollection<FilterLeafDto> Leaves { get; }

    /// <summary>
    /// The combination variant that the leaves and child nodes will be combined with.
    /// </summary>
    public LogicalCombinationVariant Combination { get; } = LogicalCombinationVariant.And;

    /// <summary>
    /// Constructor.
    /// </summary>
    [JsonConstructor]
    public CombinationFilterDto(ICollection<FilterBaseDto> childNodes, ICollection<FilterLeafDto> leaves, LogicalCombinationVariant combination)
    {
        ChildNodes = childNodes;
        Leaves = leaves;
        Combination = combination;
    }
}