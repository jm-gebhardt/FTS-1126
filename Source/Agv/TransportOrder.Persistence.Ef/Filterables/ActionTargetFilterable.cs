using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// Read model for action targets
/// </summary>
[PublicAPI]
public record ActionTargetFilterable : ActionTarget, IFilterable<ActionTarget, ActionTargetFilterable>
{
    /// <inheritdoc cref="ActionTarget.Node"/>
    [NavigationProperty]
    public new GraphNodeFilterable? Node { get; init; }

    /// <inheritdoc cref="ActionTarget.Position"/>
    [NavigationProperty]
    public new AgvPositionFilterable? Position { get; init; }
}

/// <summary>
/// Read model for agv positions.
/// </summary>
[PublicAPI]
public record AgvPositionFilterable : AgvPosition, IFilterable<AgvPosition, AgvPositionFilterable>
{
    /// <inheritdoc cref="AgvPosition.X"/>
    [FilterProperty]
    public new float X { get; init; }

    /// <inheritdoc cref="AgvPosition.Y"/>
    [FilterProperty]
    public new float Y { get; init; }

    /// <inheritdoc cref="AgvPosition.Z"/>
    [FilterProperty]
    public new float Z { get; init; }

    /// <inheritdoc cref="AgvPosition.Theta"/>
    [FilterProperty]
    public new float Theta { get; init; }
}

/// <summary>
/// ReadModel of a graph node.
/// </summary>
[PublicAPI]
public class GraphNodeFilterable : GraphNode, IFilterable<GraphNode, GraphNodeFilterable>
{
    /// <inheritdoc cref="BaseNode.Name"/>
    [FilterProperty]
    public new string Name { get; init; } = "";
}