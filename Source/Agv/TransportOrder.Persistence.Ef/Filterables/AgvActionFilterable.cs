using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <inheritdoc cref="AgvAction"/>
[PublicAPI]
public class AgvActionFilterable : AgvAction, IFilterable<AgvAction, AgvActionFilterable>
{
    /// <inheritdoc cref="AgvAction.CorrelationId"/>
    [KeyProperty]
    public new Guid CorrelationId { get; init; } 

    /// <inheritdoc cref="AgvAction.Sequence"/>
    [FilterProperty]
    public new int Sequence { get; init; }

    /// <inheritdoc cref="AgvAction.State"/>
    [FilterProperty]
    public new ActionState State { get; init; }

    /// <inheritdoc cref="AgvAction.Sequence"/>
    [FilterProperty]
    public new ActionType ActionType { get; init; }

    /// <inheritdoc cref="AgvAction.Target"/>
    [NavigationProperty]
    public new ActionTargetFilterable Target { get; init; } = null!;

    /// <inheritdoc cref="AgvAction.CreatedDate"/>
    [FilterProperty]
    public new DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="AgvAction.StartedDate"/>
    [FilterProperty]
    public new DateTime? StartedDate { get; init; }

    /// <inheritdoc cref="AgvAction.CompletedDate"/>
    [FilterProperty]
    public new DateTime? CompletedDate { get; init; }

    /// <inheritdoc cref="AgvAction.PausedDate"/>
    [FilterProperty]
    public new DateTime? PausedDate { get; init; }

    /// <inheritdoc cref="AgvAction.ShouldSkip"/>
    [FilterProperty]
    public new bool ShouldSkip { get; init; }
}