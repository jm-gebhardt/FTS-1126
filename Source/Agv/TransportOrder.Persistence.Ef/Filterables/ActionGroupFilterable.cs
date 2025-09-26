using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// Describes how an <see cref="ActionGroup"/> can be filtered.
/// </summary>
[PublicAPI]
public class ActionGroupFilterable : ActionGroup, IFilterable<ActionGroup, ActionGroupFilterable>
{
    /// <inheritdoc cref="ActionGroup.CorrelationId"/>
    [KeyProperty]
    [FilterProperty]
    public new Guid CorrelationId { get; init; }

    /// <inheritdoc cref="ActionGroup.OrderId"/>
    [FilterProperty]
    public new string OrderId { get; init; } = null!;

    /// <summary>
    /// Read model of the assigned agv.
    /// </summary>
    [JoinProperty(nameof(AssignedAgvKey), nameof(AgvFilterable.CorrelationId), true)]
    public new AgvFilterable? AssignedAgv { get; init; }

    /// <inheritdoc cref="ActionGroup.AssignedAgv"/>
    public Guid? AssignedAgvKey { get; init; }

    /// <inheritdoc cref="ActionGroup.CreatedDate"/>
    [FilterProperty]
    public new DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.StartedDate"/>
    [FilterProperty]
    public new DateTime? StartedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.CompletedDate"/>
    [FilterProperty]
    public new DateTime? CompletedDate { get; init; }

    /// <summary>
    /// The state of the ActionGroup.
    /// </summary>
    [FilterProperty]
    public new ActionGroupState State { get; init; }

    /// <inheritdoc cref="ActionGroup.IsQueued"/>
    [FilterProperty]
    public new bool IsQueued { get; init; }

    /// <inheritdoc cref="ActionGroup.Actions"/>
    [NavigationProperty]
    public new IEnumerable<AgvActionFilterable>? Actions { get; init; }

    /// <inheritdoc cref="ActionGroup.IsManual"/>
    [FilterProperty]
    public new bool IsManual { get; init; }

    /// <inheritdoc cref="ActionGroup.Creator"/>
    [FilterProperty]
    public new ActionGroupCreator Creator { get; init; }

    /// <inheritdoc cref="ActionGroup.EstimatedDuration"/>
    [FilterProperty]
    public new TimeSpan? EstimatedDuration { get; init; }

    /// <inheritdoc cref="ActionGroup.HasPriority"/>
    [FilterProperty]
    public new bool HasPriority { get; init; }

    /// <inheritdoc cref="ActionGroup.HandlingUnit"/>
    [FilterProperty]
    public new string? HandlingUnit { get; init; } 
}