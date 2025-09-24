using Gebhardt.Acc.Common.Filter.ReadModelParser;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <inheritdoc cref="AgvAction"/>
[PublicAPI]
public class AgvActionFilterReadModel : AgvAction, IFilterReadModel<AgvAction, AgvActionFilterReadModel>
{
    /// <inheritdoc cref="AgvAction.CorrelationId"/>
    [FilterReadModelBinderBase.KeyProperty]
    public new Guid CorrelationId { get; init; } 

    /// <inheritdoc cref="AgvAction.Sequence"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new int Sequence { get; init; }

    /// <inheritdoc cref="AgvAction.State"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new ActionState State { get; init; }

    /// <inheritdoc cref="AgvAction.Sequence"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new ActionType ActionType { get; init; }

    /// <inheritdoc cref="AgvAction.Target"/>
    [FilterReadModelBinderBase.NavigationProperty]
    public new ActionTargetReadModel Target { get; init; } = null!;

    /// <inheritdoc cref="AgvAction.CreatedDate"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="AgvAction.StartedDate"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new DateTime? StartedDate { get; init; }

    /// <inheritdoc cref="AgvAction.CompletedDate"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new DateTime? CompletedDate { get; init; }

    /// <inheritdoc cref="AgvAction.PausedDate"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new DateTime? PausedDate { get; init; }

    /// <inheritdoc cref="AgvAction.ShouldSkip"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new bool ShouldSkip { get; init; }
}