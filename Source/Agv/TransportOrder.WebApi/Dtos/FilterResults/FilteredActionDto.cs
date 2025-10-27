using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <summary>
/// Data transfer object for a filtered action.
/// </summary>
[PublicAPI]
public record FilteredActionDto
{
    /// <inheritdoc cref="AgvAction.CorrelationId"/>
    [SwaggerFilterableProperty]
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="AgvAction.Sequence" />
    [SwaggerFilterableProperty]
    public int Sequence { get; init; }

    /// <inheritdoc cref="AgvAction.State"/>
    [SwaggerFilterableProperty]
    public ActionState State { get; init; }

    /// <inheritdoc cref="AgvAction.ActionType"/>
    [SwaggerFilterableProperty]
    public ActionType ActionType { get; init; }

    /// <inheritdoc cref="AgvAction.Target"/>
    public FilteredActionTargetDto? Target { get; init; }

    /// <summary>
    /// The node id of the target
    /// </summary>
    public string TargetNodeId { get; init; } = null!;

    /// <summary>
    /// The node type of the target.
    /// </summary>
    public AgvGraph.NodeType TargetNodeType { get; init; } = AgvGraph.NodeType.WayPoint;

    /// <inheritdoc cref="AgvAction.CreatedDate"/>
    [SwaggerFilterableProperty]
    public DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="AgvAction.StartedDate"/>
    [SwaggerFilterableProperty]
    public DateTime? StartedDate { get; init; }

    /// <inheritdoc cref="AgvAction.CompletedDate"/>
    [SwaggerFilterableProperty]
    public DateTime? CompletedDate { get; init; }

    /// <inheritdoc cref="AgvAction.PausedDate"/>
    [SwaggerFilterableProperty]
    public DateTime? PausedDate { get; init; }

    /// <inheritdoc cref="AgvAction.Nodes"/>
    public string[] Nodes { get; init; } = null!;

    /// <inheritdoc cref="AgvAction.InfoToAgv"/>
    public Dictionary<InfoToAgvKey, string>? InfoToAgv {get; init; }

    /// <inheritdoc cref="AgvAction.IsSkipped"/>
    public bool IsSkipped { get; set; }
}