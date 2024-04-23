using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Routing.Base;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Routing;

/// <summary>
/// Considers any request plausible
/// </summary>
public class NoCheckPlausibilityChecker : IRoutingPlausibilityChecker
{
    /// <summary>
    /// No test, let it all pass
    /// </summary>
    /// <param name="start">Does not matter</param>
    /// <param name="end">Does not matter</param>
    /// <returns>True</returns>
    public bool IsPlausible(GraphNode start, GraphNode end)
    {
        return true;
    }
}