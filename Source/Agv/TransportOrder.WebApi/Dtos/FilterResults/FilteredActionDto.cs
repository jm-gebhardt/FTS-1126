using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <summary>
/// Data transfer object for a filtered action.
/// </summary>
[PublicAPI]
public record FilteredActionDto
{
    /// <summary>
    /// Unique CorrelationId of this action
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="Domain.Models.ActionGroup.AgvAction.Sequence" />
    public int Sequence { get; init; }

    /// <summary>
    /// The current state of the action
    /// </summary>
    public ActionState State { get; init; }

    /// <summary>
    /// The type of the action, e.g. Pick or Drop
    /// </summary>
    public ActionType ActionType { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public FilteredActionTargetDto? Target;

    /// <summary>
    /// 
    /// </summary>
    public AgvGraph.NodeType TargetNodeType = AgvGraph.NodeType.WayPoint;

    /// <summary>
    /// Date the action was created (UTC)
    /// </summary>
    public DateTime CreatedDate { get; init; }

    /// <summary>
    /// Date the action has been started (UTC)
    /// </summary>
    public DateTime? StartedDate { get; init; }

    /// <summary>
    /// Date the action has been completed (UTC)
    /// </summary>
    public DateTime? CompletedDate { get; init; }

    /// <summary>
    /// Date the action has been paused at (UTC)
    /// </summary>
    public DateTime? PausedDate { get; init; }

    /// <summary>
    /// Whether this <see cref="AgvAction"/> should be skipped.
    /// </summary>
    public bool ShouldSkip { get; init; }

    /// <summary>
    /// Automapper ctor.
    /// </summary>
    public FilteredActionDto()
    {
    }
}