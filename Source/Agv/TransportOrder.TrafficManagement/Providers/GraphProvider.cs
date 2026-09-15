using Gebhardt.Acc.TransportOrder.TrafficManagement.NewGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Providers;

internal class GraphProvider
{
    [UsedImplicitly]
    public RoutingNode? GetRoutingNodeById(Guid? nodeId)
    {
        return null;
    }

    [UsedImplicitly]
    public Node? GetNodeByName(string? targetNodeName)
    {
        return null;
    }
}