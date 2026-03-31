using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;

/// <summary>
/// Provides helpers for generating <see cref="GraphLayout"/> and related objects for unit testing.
/// </summary>
[PublicAPI]
public static class GraphLayoutGenerator
{
    public static GraphLayout GenerateLayout(
        string layoutId = "layout-1",
        string layoutVersion = "1.0",
        DateTime? storedAt = null,
        ICollection<GraphLayoutNode>? nodes = null,
        ICollection<GraphLayoutEdge>? edges = null,
        ICollection<GraphLayoutStation>? stations = null)
    {
        return new GraphLayout
        {
            Id = Guid.NewGuid(),
            LayoutId = layoutId,
            LayoutVersion = layoutVersion,
            LayoutName = "Test Layout",
            StoredAt = storedAt ?? DateTime.UtcNow,
            Nodes = nodes ?? [],
            Edges = edges ?? [],
            Stations = stations ?? []
        };
    }

    public static GraphLayoutNode GenerateNode(
        string nodeId,
        double x = 0,
        double y = 0,
        string? mapId = "map",
        Guid? id = null,
        params string[] vehicleTypeIds)
    {
        return new GraphLayoutNode
        {
            Id = id ?? Guid.NewGuid(),
            NodeId = nodeId,
            NodeName = nodeId,
            MapId = mapId,
            PositionX = x,
            PositionY = y,
            VehicleTypeProperties = vehicleTypeIds.Select(vt => new NodeVehicleTypeProperty
            {
                VehicleTypeId = vt,
                Theta = 0,
                Actions = []
            }).ToList()
        };
    }

    public static GraphLayoutEdge GenerateEdge(
        string edgeId,
        string startNodeId,
        string endNodeId,
        Guid? id = null,
        params string[] vehicleTypeIds)
    {
        return new GraphLayoutEdge
        {
            Id = id ?? Guid.NewGuid(),
            EdgeId = edgeId,
            StartNodeId = startNodeId,
            EndNodeId = endNodeId,
            VehicleTypeProperties = vehicleTypeIds.Select(vt => new EdgeVehicleTypeProperty
            {
                VehicleTypeId = vt,
                OrientationType = OrientationType.Global,
                Trajectory = new Trajectory
                {
                    Degree = 1,
                    KnotVector = [0f, 0f, 1f, 1f],
                    ControlPoints =
                    [
                        new ControlPoint { X = 0, Y = 0 },
                        new ControlPoint { X = 1, Y = 1 }
                    ]
                }
            }).ToList()
        };
    }

    public static GraphLayoutStation GenerateStation(
        string stationId,
        string? stationName = null,
        ICollection<string>? interactionNodeIds = null)
    {
        return new GraphLayoutStation
        {
            StationId = stationId,
            StationName = stationName ?? stationId,
            InteractionNodeIds = interactionNodeIds ?? []
        };
    }
}