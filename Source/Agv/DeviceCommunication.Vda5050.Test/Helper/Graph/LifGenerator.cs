using JetBrains.Annotations;
using Vdma.Lif;
using LifNode = Vdma.Lif.Node;
using LifEdge = Vdma.Lif.Edge;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;

/// <summary>
/// Provides helpers for generating LIF (Layout Interchange Format) objects for unit testing.
/// </summary>
[PublicAPI]
public static class LifGenerator
{
    public static Layout GenerateLayout(
        string layoutId = "layout-1",
        LifNode[]? nodes = null,
        LifEdge[]? edges = null,
        Station[]? stations = null)
    {
        return new Layout
        {
            LayoutId = layoutId,
            LayoutVersion = "1.0",
            LayoutName = "Test Layout",
            Nodes = nodes ?? [],
            Edges = edges ?? [],
            Stations = stations ?? []
        };
    }

    public static LifNode GenerateNode(
        string nodeId,
        double x = 0,
        double y = 0,
        VehicleTypeNodeProperty[]? vehicleTypeProps = null)
    {
        return new LifNode
        {
            NodeId = nodeId,
            NodeName = nodeId,
            MapId = "map",
            NodePosition = new NodePosition { X = x, Y = y },
            VehicleTypeNodeProperties = vehicleTypeProps ?? []
        };
    }

    public static LifEdge GenerateEdge(
        string edgeId,
        string startNodeId,
        string endNodeId,
        VehicleTypeEdgeProperty[]? vehicleTypeProps = null)
    {
        return new LifEdge
        {
            EdgeId = edgeId,
            StartNodeId = startNodeId,
            EndNodeId = endNodeId,
            VehicleTypeEdgeProperties = vehicleTypeProps ?? []
        };
    }
}