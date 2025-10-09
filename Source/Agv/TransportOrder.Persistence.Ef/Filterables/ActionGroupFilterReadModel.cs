using Gebhardt.Acc.Common.Filter.ReadModelParser;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// Describes how an <see cref="ActionGroup"/> can be filtered.
/// </summary>
[PublicAPI]
public class ActionGroupFilterReadModel : ActionGroup, IFilterReadModel<ActionGroup, ActionGroupFilterReadModel>
{
    /// <inheritdoc cref="ActionGroup"/>
    [Filter.KeyProperty]
    [Filter.FilterProperty]
    public new Guid CorrelationId { get; init; }

    /// <summary>
    /// A unique ID of the order that clamps all the corresponding actions
    /// </summary>
    [Filter.FilterProperty]
    public new string OrderId { get; init; } = null!;

    /// <summary>
    /// The id of the assigned agv that will execute the ActionGroup.
    /// </summary>
    [Filter.JoinProperty(nameof(AssignedAgvKey), nameof(AgvFilterReadModel.CorrelationId))]
    public new AgvFilterReadModel? AssignedAgv { get; init; }

    /// <summary>
    /// Key of <see cref="AssignedAgv"/>
    /// </summary>
    public Guid? AssignedAgvKey { get; init; }

    /// <summary>
    /// The creation date of the action (UTC).
    /// </summary>
    [Filter.FilterProperty]
    public new DateTime CreatedDate { get; init; }

    /// <summary>
    /// The date the action has been started (UTC).
    /// </summary>
    [Filter.FilterProperty]
    public new DateTime? StartedDate { get; init; }

    /// <summary>
    /// The date the action has been completed (UTC).
    /// </summary>
    [Filter.FilterProperty]
    public new DateTime? CompletedDate { get; init; }

    /// <summary>
    /// The state of the ActionGroup.
    /// </summary>
    [Filter.FilterProperty]
    public new ActionGroupState State { get; init; }

    /// <summary>
    /// True, if the action group is queued.
    /// </summary>
    [Filter.FilterProperty]
    public new bool IsQueued { get; init; }

    /// <summary>
    /// List of actions (ordered by sequence).
    /// </summary>
    [Filter.NavigationProperty]
    public new IEnumerable<AgvActionFilterReadModel>? Actions { get; init; }

    /// <summary>
    /// If true, then this ActionGroup was created manually, not by the system.
    /// </summary>
    [Filter.FilterProperty]
    public new bool IsManual { get; init; }

    /// <summary>
    /// The creator of the <see cref="ActionGroup"/>.
    /// </summary>
    [Filter.FilterProperty]
    public new ActionGroupCreator Creator { get; init; }

    /// <summary>
    /// The estimated duration based on the estimated duration of the actions.
    /// </summary>
    [Filter.FilterProperty]
    public new TimeSpan? EstimatedDuration { get; init; }

    /// <summary>
    /// If true, then this ActionGroup has priority over others (starts immediately).
    /// </summary>
    [Filter.FilterProperty]
    public new bool HasPriority { get; init; }

    /// <summary>
    /// The handling unit corresponding to this ActionGroup.
    /// </summary>
    [Filter.FilterProperty]
    public new string? HandlingUnit { get; init; } 

    /// <summary>
    /// Automapper ctor.
    /// </summary>
    public ActionGroupFilterReadModel() 
    {
    }
}