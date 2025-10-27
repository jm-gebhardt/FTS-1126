using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Domain read model for actions.
/// </summary>
[PublicAPI]
public record FilteredAgvAction
{
    /// <inheritdoc cref="AgvAction.CorrelationId"/>
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="AgvAction.Sequence"/>
    public int Sequence { get; init; }

    /// <inheritdoc cref="AgvAction.State"/>
    public ActionState State { get; init; }

    /// <inheritdoc cref="AgvAction.ActionType"/>
    public ActionType ActionType { get; init; }

    /// <inheritdoc cref="AgvAction.Target"/>
    public FilteredActionTarget Target { get; init; } = null!;

    /// <inheritdoc cref="AgvAction.CreatedDate"/>
    public DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="AgvAction.StartedDate"/>
    public DateTime? StartedDate { get; init; }

    /// <inheritdoc cref="AgvAction.CompletedDate"/>
    public DateTime? CompletedDate { get; init; }

    /// <inheritdoc cref="AgvAction.PausedDate"/>
    public DateTime? PausedDate { get; init; }

    /// <inheritdoc cref="AgvAction.IsSkipped"/>
    public bool ShouldSkip { get; init; }

    /// <inheritdoc cref="AgvAction.Nodes"/>
    public GraphNode[] Nodes { get; init; } = null!;

    /// <inheritdoc cref="AgvAction.InfoToAgv"/>
    public Dictionary<InfoToAgvKey, string>? InfoToAgv { get; init; }
}