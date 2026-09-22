using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Agvs;

/// <summary>
/// Provider for AGV information. Holds relevant information in-memory and updates it based on incoming events. This is a placeholder for later implementation.
/// </summary>
internal class AgvProvider
{
    [UsedImplicitly]
    public IRoutingAgv GetAgv(Guid agvCorrelationId)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Interface as placeholder for later defined Agv status for traffic management.
/// </summary>
internal interface IRoutingAgv
{
    Guid CorrelationId { get; }

    RoutingNode? CurrentRoutingNode { get; }

    bool IsInAutomaticMode { get; }
}