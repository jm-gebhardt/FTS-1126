using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Filter.ReadModelParser;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// Read model for action targets
/// </summary>
[PublicAPI]
public record ActionTargetReadModel : ActionTarget, IFilterReadModel<ActionTarget, ActionTargetReadModel>
{
    /// <inheritdoc cref="ActionTarget.Node"/>
    [FilterReadModelBinderBase.NavigationProperty]
    public new GraphNodeReadModel? Node { get; init; }

    /// <inheritdoc cref="ActionTarget.Position"/>
    [FilterReadModelBinderBase.NavigationProperty]
    public new AgvPositionReadModel? Position { get; init; }
}

/// <summary>
/// Read model for agv positions.
/// </summary>
[PublicAPI]
public record AgvPositionReadModel : AgvPosition, IFilterReadModel<AgvPosition, AgvPositionReadModel>
{
    /// <inheritdoc cref="AgvPosition.X"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new float X { get; init; }

    /// <inheritdoc cref="AgvPosition.Y"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new float Y { get; init; }

    /// <inheritdoc cref="AgvPosition.Z"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new float Z { get; init; }

    /// <inheritdoc cref="AgvPosition.Theta"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new float Theta { get; init; }
}

/// <summary>
/// ReadModel of a graph node.
/// </summary>
[PublicAPI]
public class GraphNodeReadModel : GraphNode, IFilterReadModel<GraphNode, GraphNodeReadModel>
{
    /// <inheritdoc cref="GraphNode.Name"/>
    [FilterReadModelBinderBase.FilterProperty]
    public new string Name { get; init; } = "";
}