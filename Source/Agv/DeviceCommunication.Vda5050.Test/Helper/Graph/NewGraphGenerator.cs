using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;

public static class NewGraphGenerator
{
    public const string VehicleTypeId = "vt1";
    public const string OtherVehicleTypeId = "vt2";
    public const string OtherLayoutId = "OtherLayoutId";
    public const string LevelId = "Level1";
    public const string StartNodeId = "N1";
    public const string EndNodeId = "N2";
    public const string EdgeId = "E1";
    public const string StationId = "StationId";

    private const string ProjectIdentification = "TestProject";
    private const string LayoutId = "LayoutId";
    private const string LayoutVersion = "V1";
    private const string FileName = "lif.json";
    private const string UploadedBy = "user";

    public static LifLayoutCollection SampleCollection { get; } = GenerateCollection(GenerateLayout());

    private static MetaInformation GenerateMetaInformation()
    {
        return new MetaInformation("Test", DateTimeOffset.UtcNow, LayoutVersion, ProjectIdentification);
    }

    private static Node GenerateNode(string nodeId, double x, double y, string vehicleTypeId = VehicleTypeId)
    {
        return new Node(nodeId, new NodePosition(x, y), [new VehicleTypeNodeProperty(Guid.NewGuid(), vehicleTypeId)], nodeName: nodeId);
    }

    public static Layout GenerateLayout(string layoutId = LayoutId, string? layoutLevelId = LevelId, string vehicleTypeId = VehicleTypeId)
    {
        return new Layout(
            Guid.NewGuid(),
            layoutId,
            LayoutVersion,
            [new Edge(EdgeId, EndNodeId, StartNodeId, [new VehicleTypeEdgeProperty(Guid.NewGuid(), vehicleTypeId)])],
            [GenerateNode(StartNodeId, 1, 2, vehicleTypeId), GenerateNode(EndNodeId, 2, 1, vehicleTypeId)],
            [new Station(Guid.NewGuid(), StationId, [StartNodeId], [Guid.NewGuid()], stationPosition: new StationPosition(1, 1))],
            layoutLevelId: layoutLevelId);
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

    public static Guid GenerateInternalId(string lifId, string layoutId = LayoutId)
    {
        return LifHelper.GenerateInternalId(lifId, layoutId, ProjectIdentification);
    }
}