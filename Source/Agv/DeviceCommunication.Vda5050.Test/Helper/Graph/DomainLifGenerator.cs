using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;

/// <summary>
/// DomainLifGenerator generates LIF object of the namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif
/// 
/// </summary>
public static class DomainLifGenerator
{
    private const string VehicleTypeId = "vt1";
    private const string OtherVehicleTypeId = "vt2";
    public const string OtherLayoutId = "OtherLayoutId";
    public const string LevelId = "Level1";
    public const string StartNodeId = "N1";
    public const string EndNodeId = "N2";
    public const string EdgeId = "E1";
    public const string StationId = "StationId";

    private const string ProjectIdentification = "TestProject";
    private const string Creator = "Gebhardt";
    private const string LayoutId = "LayoutId";
    private const string LayoutVersion = "V1";
    private const string FileName = "lif.json";
    private const string UploadedBy = "user";

    public static LifLayoutCollection SampleCollection { get; } = GenerateCollection(GenerateLayout());

    public static Dictionary<string, Guid> AgvTypeIdentifiers { get; } = new() { { VehicleTypeId, Guid.NewGuid() }, { OtherVehicleTypeId, Guid.NewGuid() } };

    private static MetaInformation GenerateMetaInformation()
    {
        return new MetaInformation(Creator, DateTimeOffset.UtcNow, LayoutVersion, ProjectIdentification);
    }

    private static Node GenerateNode(string nodeId, double x, double y, string vehicleTypeId = VehicleTypeId)
    {
        return new Node(nodeId, new NodePosition(x, y), [new VehicleTypeNodeProperty(Guid.NewGuid(), vehicleTypeId)], nodeName: nodeId);
    }

    public static Layout GenerateLayout(string layoutId = LayoutId, string? layoutLevelId = LevelId, string vehicleTypeId = VehicleTypeId)
    {
        string startNodeId = BuildLayoutScopedId(StartNodeId, layoutId);
        string endNodeId = BuildLayoutScopedId(EndNodeId, layoutId);
        string edgeId = BuildLayoutScopedId(EdgeId, layoutId);
        string stationId = BuildLayoutScopedId(StationId, layoutId);

        return new Layout(
            Guid.NewGuid(),
            layoutId,
            LayoutVersion,
            [new Edge(edgeId, endNodeId, startNodeId, [new VehicleTypeEdgeProperty(Guid.NewGuid(), vehicleTypeId)])],
            [GenerateNode(startNodeId, 1, 2, vehicleTypeId), GenerateNode(endNodeId, 2, 1, vehicleTypeId)],
            [new Station(Guid.NewGuid(), stationId, [startNodeId], [Guid.NewGuid()], stationPosition: new StationPosition(1, 1))],
            layoutLevelId: layoutLevelId);
    }

    /// <summary>
    /// Ensures node, edge and station ids are unique across layouts with different layout ids while keeping the ids of the default layout unchanged.
    /// </summary>
    private static string BuildLayoutScopedId(string baseId, string layoutId)
    {
        return layoutId == LayoutId ? baseId : $"{baseId}_{layoutId}";
    }

    public static LifLayoutCollection GenerateCollection(params Layout[] layouts)
    {
        return new LifLayoutCollection(layouts, GenerateMetaInformation());
    }

    public static LifFile GenerateLifFile(DateTime uploadTimestamp, ICollection<string> supportedVehicleTypes, ICollection<Guid>? layoutIds = null)
    {
        return new LifFile(
            Guid.NewGuid(),
            GenerateMetaInformation(),
            layoutIds ?? [],
            supportedVehicleTypes,
            new LifFileMetaInformation(uploadTimestamp, FileName, 1, UploadedBy));
    }

    public static Guid GenerateInternalNodeId(string nodeId, string layoutId = LayoutId)
    {
        return InternalIdGenerator.GetNodeId(Creator, BuildLayoutScopedId(nodeId, layoutId), out _);
    }

    public static Guid GenerateInternalEdgeId(string edgeId, string layoutId = LayoutId)
    {
        return InternalIdGenerator.GetEdgeId(Creator, BuildLayoutScopedId(edgeId, layoutId), out _);
    }

    public static Guid GenerateInternalStationId(string stationId, string layoutId = LayoutId)
    {
        return InternalIdGenerator.GetStationId(Creator, BuildLayoutScopedId(stationId, layoutId), out _);
    }
}