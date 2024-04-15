using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Base;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;

/// <inheritdoc />
public class RoutingPlausibilityChecker : IRoutingPlausibilityChecker
{
    /// <summary>
    /// When the start or end node is an action type, then we do additional plausibility checks (otherwise it's considered plausible)
    /// Is the starting point a Source? Is the destination a target?<br/>
    /// - Source to Destination (valid)<br/>
    /// - Source to Source (not valid)<br/>
    /// - Destination to Source (not valid)<br/>
    /// - Destination to Destination (not valid)
    /// </summary>
    /// <param name="start">The source node</param>
    /// <param name="end">The destination node</param>
    /// <returns>True if none is a action type (Pick, Drop, Pick/Drop) or if the start is a source and the end is a destination<br/>
    /// Otherwise False</returns>
    public bool IsPlausible(GraphNode start, GraphNode end)
    {
        if (!NodeTypeHelper.IsActionNodeType(start.Type) && !NodeTypeHelper.IsActionNodeType(end.Type))
        {
            return true;
        }
        return NodeTypeHelper.IsSourceNodeType(start.Type) && NodeTypeHelper.IsDestinationNodeType(end.Type);
    }
}