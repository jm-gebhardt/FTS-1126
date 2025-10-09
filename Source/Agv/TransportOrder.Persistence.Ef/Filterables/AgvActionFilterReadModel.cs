using Gebhardt.Acc.Common.Filter.ReadModelParser;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// 
/// </summary>
[PublicAPI]
public class AgvActionFilterReadModel : AgvAction, IFilterReadModel<AgvAction, AgvActionFilterReadModel>
{
    /// <summary>
    /// Unique CorrelationId of this action
    /// </summary>
    [Filter.KeyProperty]
    public new Guid CorrelationId { get; init; } 

    /// <inheritdoc cref="Domain.Models.ActionGroup.AgvAction" />
    [Filter.FilterProperty]
    public new int Sequence { get; init; } 

    /// <summary>
    /// The current state of the action
    /// </summary>
    [Filter.FilterProperty]
    public new ActionState State { get; init; }

    /// <summary>
    /// The type of the action, e.g. Pick or Drop
    /// </summary>
    [Filter.FilterProperty]
    public new ActionType ActionType { get; init; } 

    /// <summary>
    /// The target of the action.
    /// </summary>
    [Filter.NavigationProperty]
    public new ActionTargetReadModel Target { get; init; } = null!;

    /// <summary>
    /// Date the action was created (UTC)
    /// </summary>
    [Filter.FilterProperty]
    public new DateTime CreatedDate { get; init; } 

    /// <summary>
    /// Date the action has been started (UTC)
    /// </summary>
    [Filter.FilterProperty]
    public new DateTime? StartedDate { get; init; } 

    /// <summary>
    /// Date the action has been completed (UTC)
    /// </summary>
    [Filter.FilterProperty]
    public new DateTime? CompletedDate { get; init; } 

    /// <summary>
    /// Date the action has been paused at (UTC)
    /// </summary>
    [Filter.FilterProperty]
    public new DateTime? PausedDate { get; init; }

    /// <summary>
    /// Whether this <see cref="AgvAction"/> should be skipped.
    /// </summary>
    [Filter.FilterProperty]
    public new bool ShouldSkip { get; init; }

    /// <summary>
    /// Automapper ctor.
    /// </summary>
    public AgvActionFilterReadModel()
    {
    }
}