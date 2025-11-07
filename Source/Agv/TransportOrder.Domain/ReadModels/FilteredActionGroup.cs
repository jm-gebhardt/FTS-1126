using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Domain read model of filterable action groups.
/// </summary>
[PublicAPI]
public record FilteredActionGroup
{
    /// <inheritdoc cref="ActionGroup.CorrelationId"/>
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="ActionGroup.OrderId"/>
    public string OrderId { get; init; } = null!;

    /// <inheritdoc cref="ActionGroup.AssignedAgv"/>
    public FilteredAgv? AssignedAgv { get; init; }

    /// <inheritdoc cref="ActionGroup.CreatedDate"/>
    public DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.StartedDate"/>
    public DateTime? StartedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.CompletedDate"/>
    public DateTime? CompletedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.State"/>
    public ActionGroupState State { get; init; }

    /// <inheritdoc cref="ActionGroup.IsQueued"/>
    public bool IsQueued { get; init; }

    /// <inheritdoc cref="ActionGroup.Actions"/>
    public ICollection<FilteredAgvAction>? Actions { get; init; }

    /// <inheritdoc cref="ActionGroup.IsManual"/>
    public bool IsManual { get; init; }

    /// <inheritdoc cref="ActionGroup.Creator"/>
    public ActionGroupCreator Creator { get; init; }

    /// <inheritdoc cref="ActionGroup.EstimatedDuration"/>
    public TimeSpan? EstimatedDuration { get; init; }

    /// <inheritdoc cref="ActionGroup.HandlingUnit"/>
    public string? HandlingUnit { get; init; }

    /// <inheritdoc cref="ActionGroup.Reasons"/>
    public List<Reason> Reasons { get; init; } = null!;

    /// <inheritdoc cref="ActionGroup.DoNotPause"/>
    public bool DoNotPause { get; init; }

    /// <inheritdoc cref="ActionGroup.ShouldEscapeQueue"/>
    public bool ShouldEscapeQueue { get; init; }

    /// <inheritdoc cref="ActionGroup.Priority"/>
    public Priority Priority { get; init; }
}