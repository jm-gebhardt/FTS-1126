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

    public static MetaInformation GenerateMetaInformation(string lifVersion = "1.0.0")
    {
        return new MetaInformation
        {
            Creator = "Test",
            ExportTimestamp = DateTimeOffset.UtcNow,
            LifVersion = lifVersion,
            ProjectIdentification = "Test Project"
        };
    }

    public static Station GenerateStation(string stationId, string[] interactionNodeIds, StationPosition? stationPosition = null)
    {
        return new Station
        {
            StationId = stationId,
            InteractionNodeIds = interactionNodeIds,
            StationPosition = stationPosition
        };
    }

    public static VehicleTypeEdgeProperty GenerateVehicleTypeEdgeProperty(
        string vehicleTypeId = "vt1",
        LoadRestriction? loadRestriction = null,
        VehicleTypeEdgePropertyAction[]? actions = null,
        string? orientationType = null,
        bool? rotationAllowed = null,
        string? rotationAtStartNodeAllowed = null,
        string? rotationAtEndNodeAllowed = null,
        double? vehicleOrientation = null,
        double? maxHeight = null,
        double? minHeight = null,
        double? maxSpeed = null,
        double? maxRotationSpeed = null,
        Trajectory? trajectory = null)
    {
        return new VehicleTypeEdgeProperty
        {
            VehicleTypeId = vehicleTypeId,
            LoadRestriction = loadRestriction,
            Actions = actions,
            OrientationType = orientationType,
            RotationAllowed = rotationAllowed,
            RotationAtStartNodeAllowed = rotationAtStartNodeAllowed,
            RotationAtEndNodeAllowed = rotationAtEndNodeAllowed,
            VehicleOrientation = vehicleOrientation,
            MaxHeight = maxHeight,
            MinHeight = minHeight,
            MaxSpeed = maxSpeed,
            MaxRotationSpeed = maxRotationSpeed,
            Trajectory = trajectory
        };
    }

    public static VehicleTypeNodeProperty GenerateVehicleTypeNodeProperty(
        string vehicleTypeId = "vt1",
        VehicleTypeNodePropertyAction[]? actions = null,
        double? theta = null)
    {
        return new VehicleTypeNodeProperty
        {
            VehicleTypeId = vehicleTypeId,
            Actions = actions,
            Theta = theta
        };
    }

    public static LoadRestriction GenerateLoadRestriction(bool loaded, bool unloaded, string[]? loadSetNames = null)
    {
        return new LoadRestriction
        {
            Loaded = loaded,
            Unloaded = unloaded,
            LoadSetNames = loadSetNames
        };
    }

    public static VehicleTypeEdgePropertyAction GenerateEdgeAction(string actionType)
    {
        return new VehicleTypeEdgePropertyAction
        {
            ActionType = actionType,
            BlockingType = BlockingType.None
        };
    }

    public static VehicleTypeNodePropertyAction GenerateNodeAction(string actionType)
    {
        return new VehicleTypeNodePropertyAction
        {
            ActionType = actionType,
            BlockingType = BlockingType.None
        };
    }
}