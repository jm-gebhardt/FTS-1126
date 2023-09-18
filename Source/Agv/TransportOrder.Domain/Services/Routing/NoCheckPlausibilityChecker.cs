using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using TransportOrder.Routing.Base;

namespace TransportOrder.Domain.Services.Routing;

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