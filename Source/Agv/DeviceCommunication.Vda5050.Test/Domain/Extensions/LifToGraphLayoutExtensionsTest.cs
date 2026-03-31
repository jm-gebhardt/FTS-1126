using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;
using Vdma.Lif;
using CommonBlockingType = Gebhardt.Acc.Common.Domain.Models.BlockingType;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class LifToGraphLayoutExtensionsTest
{
    private static readonly DateTime _storedAt = new(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void ToGraphLayouts_MapsAllLayouts()
    {
        var lif = new LifLayoutCollection
        {
            Layouts =
            [
                LifGenerator.GenerateLayout("L1", nodes: [LifGenerator.GenerateNode("n1")], edges: [LifGenerator.GenerateEdge("e1", "n1", "n1")]),
                LifGenerator.GenerateLayout("L2", nodes: [LifGenerator.GenerateNode("n2")], edges: [LifGenerator.GenerateEdge("e2", "n2", "n2")])
            ]
        };

        ICollection<GraphLayout> result = lif.ToGraphLayouts(_storedAt);

        result.Count.ShouldBe(2);
        result.ShouldContain(l => l.LayoutId == "L1");
        result.ShouldContain(l => l.LayoutId == "L2");
    }

    [TestMethod]
    public void ToGraphLayouts_SetsStoredAtOnAll()
    {
        var lif = new LifLayoutCollection
        {
            Layouts = [LifGenerator.GenerateLayout(nodes: [LifGenerator.GenerateNode("n1")])]
        };

        ICollection<GraphLayout> result = lif.ToGraphLayouts(_storedAt);

        result.Single().StoredAt.ShouldBe(_storedAt);
    }

    [TestMethod]
    public void ToGraphLayoutNode_DeterministicGuid()
    {
        var lifNode = LifGenerator.GenerateNode("test-node-id");

        var result = lifNode.ToGraphLayoutNode();

        result.Id.ShouldBe("test-node-id".ToGuid());
    }

    [TestMethod]
    public void ToGraphLayoutNode_MapsPositionAndMapId()
    {
        var lifNode = LifGenerator.GenerateNode("n1", x: 5.5, y: 10.2);

        var result = lifNode.ToGraphLayoutNode();

        result.PositionX.ShouldBe(5.5);
        result.PositionY.ShouldBe(10.2);
        result.MapId.ShouldBe("map");
        result.NodeName.ShouldBe("n1");
    }

    [TestMethod]
    public void ToGraphLayoutNode_MapsVehicleTypeProperties()
    {
        var lifNode = LifGenerator.GenerateNode("n1", vehicleTypeProps:
        [
            new VehicleTypeNodeProperty
            {
                VehicleTypeId = "Maker.Series1",
                Theta = 1.5,
                Actions = [new VehicleTypeNodePropertyAction { ActionType = "pick", BlockingType = BlockingType.Hard }]
            }
        ]);

        var result = lifNode.ToGraphLayoutNode();

        result.VehicleTypeProperties.Count.ShouldBe(1);
        var prop = result.VehicleTypeProperties.Single();
        prop.VehicleTypeId.ShouldBe("Maker.Series1");
        prop.Theta.ShouldBe(1.5);
        prop.Actions.Count.ShouldBe(1);
        prop.Actions.Single().ActionType.ShouldBe("pick");
        prop.Actions.Single().BlockingType.ShouldBe(CommonBlockingType.HARD);
    }

    [TestMethod]
    public void ToGraphLayoutEdge_DeterministicGuid()
    {
        var lifEdge = LifGenerator.GenerateEdge("test-edge-id", "n1", "n2");

        var result = lifEdge.ToGraphLayoutEdge();

        result.Id.ShouldBe("test-edge-id".ToGuid());
    }

    [TestMethod]
    public void ToGraphLayoutEdge_MapsStartEndNodes()
    {
        var lifEdge = LifGenerator.GenerateEdge("e1", "start", "end");

        var result = lifEdge.ToGraphLayoutEdge();

        result.StartNodeId.ShouldBe("start");
        result.EndNodeId.ShouldBe("end");
    }

    [TestMethod]
    public void ToGraphLayoutEdge_MapsTrajectory()
    {
        var lifEdge = LifGenerator.GenerateEdge("e1", "n1", "n2", vehicleTypeProps:
        [
            new VehicleTypeEdgeProperty
            {
                VehicleTypeId = "Maker.S1",
                Trajectory = new Vdma.Lif.Trajectory
                {
                    Degree = 3,
                    KnotVector = [0, 0, 0, 0, 1, 1, 1, 1],
                    ControlPoints =
                    [
                        new Vdma.Lif.ControlPoint { X = 1.0, Y = 2.0, Weight = 1.0 },
                        new Vdma.Lif.ControlPoint { X = 3.0, Y = 4.0, Weight = 1.0 }
                    ]
                }
            }
        ]);

        var result = lifEdge.ToGraphLayoutEdge();

        var prop = result.VehicleTypeProperties.Single();
        prop.Trajectory.ShouldNotBeNull();
        prop.Trajectory!.Degree.ShouldBe(3);
        prop.Trajectory.ControlPoints.Count.ShouldBe(2);
    }

    [TestMethod]
    public void ToGraphLayoutStation_MapsPositionAndInteractionNodes()
    {
        var station = new Station
        {
            StationId = "s1",
            StationName = "Station 1",
            StationDescription = "desc",
            StationPosition = new StationPosition { X = 1.0, Y = 2.0, Theta = 3.14 },
            InteractionNodeIds = ["n1", "n2"]
        };

        var result = station.ToGraphLayoutStation();

        result.StationId.ShouldBe("s1");
        result.StationName.ShouldBe("Station 1");
        result.StationDescription.ShouldBe("desc");
        result.PositionX.ShouldBe(1.0);
        result.PositionY.ShouldBe(2.0);
        result.Theta.ShouldBe(3.14);
        result.InteractionNodeIds.Count.ShouldBe(2);
    }

    [TestMethod]
    public void ToGraphLayout_MapsNodeEdgeStationCounts()
    {
        var layout = LifGenerator.GenerateLayout(
            nodes: [LifGenerator.GenerateNode("n1"), LifGenerator.GenerateNode("n2")],
            edges: [LifGenerator.GenerateEdge("e1", "n1", "n2")],
            stations: [new Station { StationId = "s1", InteractionNodeIds = [] }]);

        var result = layout.ToGraphLayout(_storedAt);

        result.Nodes.Count.ShouldBe(2);
        result.Edges.Count.ShouldBe(1);
        result.Stations.Count.ShouldBe(1);
        result.LayoutId.ShouldBe("layout-1");
        result.LayoutVersion.ShouldBe("1.0");
    }
}