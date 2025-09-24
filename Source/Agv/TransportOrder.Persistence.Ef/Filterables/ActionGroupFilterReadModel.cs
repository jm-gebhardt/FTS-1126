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
    /// <inheritdoc cref="ActionGroup.CorrelationId"/>
    [FilterReadModelBinderBase.KeyProperty]
    [FilterReadModelBinderBase.FilterProperty]
    public new Guid CorrelationId { get; init; }

    /// <inheritdoc cref="ActionGroup.OrderId"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new string OrderId { get; init; } = null!;

    /// <summary>
    /// Read model of the assigned agv.
    /// </summary>
    [FilterReadModelBinderBase.JoinProperty(nameof(AssignedAgvKey), nameof(AgvFilterReadModel.CorrelationId), true)]
    public new AgvFilterReadModel? AssignedAgv { get; init; }

    /// <inheritdoc cref="ActionGroup.AssignedAgv"/>
    public Guid? AssignedAgvKey { get; init; }

    /// <inheritdoc cref="ActionGroup.CreatedDate"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.StartedDate"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new DateTime? StartedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.CompletedDate"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new DateTime? CompletedDate { get; init; }

    /// <summary>
    /// The state of the ActionGroup.
    /// </summary>
    [FilterReadModelBinderBase.FilterProperty]
    public new ActionGroupState State { get; init; }

    /// <inheritdoc cref="ActionGroup.IsQueued"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new bool IsQueued { get; init; }

    /// <inheritdoc cref="ActionGroup.Actions"/>
    [FilterReadModelBinderBase.NavigationProperty]
    public new IEnumerable<AgvActionFilterReadModel>? Actions { get; init; }

    /// <inheritdoc cref="ActionGroup.IsManual"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new bool IsManual { get; init; }

    /// <inheritdoc cref="ActionGroup.Creator"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new ActionGroupCreator Creator { get; init; }

    /// <inheritdoc cref="ActionGroup.EstimatedDuration"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new TimeSpan? EstimatedDuration { get; init; }

    /// <inheritdoc cref="ActionGroup.HasPriority"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new bool HasPriority { get; init; }

    /// <inheritdoc cref="ActionGroup.HandlingUnit"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new string? HandlingUnit { get; init; } 
}