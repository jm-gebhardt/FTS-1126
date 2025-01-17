using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Routing.Base;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Routing;

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
    /// <returns>True if none is an action type (Pick, Drop, Pick/Drop) or if the start is a source and the end is a destination<br/>
    /// Otherwise False</returns>
    public bool IsPlausible(GraphNode start, GraphNode end)
    {
        if (!(start.IsPickStation() || start.IsDropStation())&& !(end.IsPickStation() || end.IsDropStation()))
        {
            return true;
        }
        return start.IsPickStation() && end.IsDropStation();
    }
}