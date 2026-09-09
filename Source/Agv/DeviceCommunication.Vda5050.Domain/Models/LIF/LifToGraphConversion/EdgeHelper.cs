using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.LIF.LifToGraphConversion;

internal class EdgeHelper
{
    public Edge Edge { get; }

    public IReadOnlyList<VehicleTypeEdgePropertyHelper> EdgePropertyHelpers { get; }

    public EdgeHelper(Edge edge)
    {
        Edge = edge;
        EdgePropertyHelpers = edge.VehicleTypeEdgeProperties.Select(p => new VehicleTypeEdgePropertyHelper(p)).ToList();
    }
}