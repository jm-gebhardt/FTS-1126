using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Domain read model for actions.
/// </summary>
[PublicAPI]
public record FilteredAgvAction
{
    /// <summary>
    /// Unique CorrelationId of this action
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The position of this action in the action group sequence.
    /// </summary>
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
    /// The target of the action.
    /// </summary>
    public FilteredActionTarget Target { get; init; } = null!;

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
}