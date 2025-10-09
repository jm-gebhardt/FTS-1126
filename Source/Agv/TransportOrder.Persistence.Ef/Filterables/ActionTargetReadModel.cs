using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Filter.ReadModelParser;
using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;

/// <summary>
/// Read model for action targets
/// </summary>
public record ActionTargetReadModel : ActionTarget, IFilterReadModel<ActionTarget, ActionTargetReadModel>
{
    /// <summary>
    /// 
    /// </summary>
    [Filter.NavigationProperty]
    public new GraphNodeReadModel? Node { get; init; }

    /// <summary>
    /// 
    /// </summary>
    [Filter.NavigationProperty]
    public new AgvPositionReadModel? Position { get; init; }
}

/// <summary>
/// 
/// </summary>
public record AgvPositionReadModel : AgvPosition, IFilterReadModel<AgvPosition, AgvPositionReadModel>
{
    /// <summary>
    /// Current X-position [m].
    /// </summary>
    [Filter.FilterProperty]
    public new float X { get; init; }

    /// <summary>
    /// Current Y-position [m].
    /// </summary>
    [Filter.FilterProperty]
    public new float Y { get; init; }

    /// <summary>
    /// Current Z-position [m].
    /// </summary>
    [Filter.FilterProperty]
    public new float Z { get; init; }

    /// <summary>
    /// Current angle (rotation) [rad].
    /// </summary>
    [Filter.FilterProperty]
    public new float Theta { get; init; }
}

/// <summary>
/// 
/// </summary>
public class GraphNodeReadModel : GraphNode, IFilterReadModel<GraphNode, GraphNodeReadModel>
{
    /// <summary>
    /// 
    /// </summary>
    [Filter.FilterProperty]
    public new string Name { get; init; } = "";
}