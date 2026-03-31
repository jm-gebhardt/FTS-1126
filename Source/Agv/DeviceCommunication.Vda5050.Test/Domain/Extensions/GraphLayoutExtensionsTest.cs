using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class GraphLayoutExtensionsTest
{
    private const string VehicleTypeA = "Maker.SeriesA";
    private const string VehicleTypeB = "Maker.SeriesB";

    [TestMethod]
    public void ToAgvGraph_FiltersNodesByVehicleType()
    {
        var agvId = Guid.NewGuid();
        var node1 = GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: VehicleTypeA);
        var node2 = GraphLayoutGenerator.GenerateNode("n2", vehicleTypeIds: VehicleTypeB);
        ICollection<GraphLayout> layouts = [GraphLayoutGenerator.GenerateLayout(nodes: [node1, node2])];

        var graph = layouts.ToAgvGraph(VehicleTypeA, agvId);

        graph.Nodes.Count.ShouldBe(1);
        graph.Nodes.Single().NodeId.ShouldBe("n1");
        graph.AgvCorrelationId.ShouldBe(agvId);
    }

    [TestMethod]
    public void ToAgvGraph_FiltersEdgesByVehicleType()
    {
        var agvId = Guid.NewGuid();
        var node1 = GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: VehicleTypeA);
        var node2 = GraphLayoutGenerator.GenerateNode("n2", vehicleTypeIds: VehicleTypeA);
        var edge1 = GraphLayoutGenerator.GenerateEdge("e1", "n1", "n2", vehicleTypeIds: VehicleTypeA);
        var edge2 = GraphLayoutGenerator.GenerateEdge("e2", "n1", "n2", vehicleTypeIds: VehicleTypeB);
        ICollection<GraphLayout> layouts = [GraphLayoutGenerator.GenerateLayout(nodes: [node1, node2], edges: [edge1, edge2])];

        var graph = layouts.ToAgvGraph(VehicleTypeA, agvId);

        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Single().EdgeId.ShouldBe("e1");
    }

    [TestMethod]
    public void ToAgvGraph_NoMatchingVehicleType_ReturnsEmptyGraph()
    {
        var agvId = Guid.NewGuid();
        var node = GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: VehicleTypeB);
        ICollection<GraphLayout> layouts = [GraphLayoutGenerator.GenerateLayout(nodes: [node])];

        var graph = layouts.ToAgvGraph(VehicleTypeA, agvId);

        graph.Nodes.ShouldBeEmpty();
        graph.Edges.ShouldBeEmpty();
    }

    [TestMethod]
    public void ToAgvGraph_MultipleLayouts_MergesAll()
    {
        var agvId = Guid.NewGuid();
        var node1 = GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: VehicleTypeA);
        var node2 = GraphLayoutGenerator.GenerateNode("n2", vehicleTypeIds: VehicleTypeA);
        ICollection<GraphLayout> layouts =
        [
            GraphLayoutGenerator.GenerateLayout("L1", nodes: [node1]),
            GraphLayoutGenerator.GenerateLayout("L2", nodes: [node2])
        ];

        var graph = layouts.ToAgvGraph(VehicleTypeA, agvId);

        graph.Nodes.Count.ShouldBe(2);
    }

    [TestMethod]
    public void GetVehicleTypeIds_ReturnsDistinctSet()
    {
        var node1 = GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: [VehicleTypeA, VehicleTypeB]);
        var node2 = GraphLayoutGenerator.GenerateNode("n2", vehicleTypeIds: VehicleTypeA);
        var edge = GraphLayoutGenerator.GenerateEdge("e1", "n1", "n2", vehicleTypeIds: VehicleTypeB);
        ICollection<GraphLayout> layouts = [GraphLayoutGenerator.GenerateLayout(nodes: [node1, node2], edges: [edge])];

        HashSet<string> ids = layouts.GetVehicleTypeIds();

        ids.Count.ShouldBe(2);
        ids.ShouldContain(VehicleTypeA);
        ids.ShouldContain(VehicleTypeB);
    }

    [TestMethod]
    public void ToNode_VehicleTypeNotPresent_ReturnsNull()
    {
        var layoutNode = GraphLayoutGenerator.GenerateNode("n1", vehicleTypeIds: VehicleTypeB);

        var result = layoutNode.ToNode(VehicleTypeA, Guid.NewGuid(), "layout-1");

        result.ShouldBeNull();
    }

    [TestMethod]
    public void ToNode_VehicleTypePresent_SetsPositionAndMapId()
    {
        var layoutNode = GraphLayoutGenerator.GenerateNode("n1", x: 5.0, y: 10.0, mapId: "myMap", vehicleTypeIds: VehicleTypeA);

        var result = layoutNode.ToNode(VehicleTypeA, Guid.NewGuid(), "fallback");

        result.ShouldNotBeNull();
        result.Position.X.ShouldBe(5.0);
        result.Position.Y.ShouldBe(10.0);
        result.MapId.ShouldBe("myMap");
    }

    [TestMethod]
    public void ToNode_NoMapId_FallsBackToLayoutId()
    {
        var layoutNode = GraphLayoutGenerator.GenerateNode("n1", mapId: null, vehicleTypeIds: VehicleTypeA);

        var result = layoutNode.ToNode(VehicleTypeA, Guid.NewGuid(), "fallback-layout");

        result.ShouldNotBeNull();
        result.MapId.ShouldBe("fallback-layout");
    }

    [TestMethod]
    public void ToNode_WithActions_MapsActionsCorrectly()
    {
        var agvId = Guid.NewGuid();
        var layoutNode = new GraphLayoutNode
        {
            Id = Guid.NewGuid(),
            NodeId = "n1",
            NodeName = "n1",
            PositionX = 0,
            PositionY = 0,
            VehicleTypeProperties =
            [
                new NodeVehicleTypeProperty
                {
                    VehicleTypeId = VehicleTypeA,
                    Theta = 1.5,
                    Actions =
                    [
                        new NodeVehicleTypeAction
                        {
                            ActionType = "pick",
                            BlockingType = BlockingType.HARD,
                            RequirementType = RequirementType.Required
                        }
                    ]
                }
            ]
        };

        var result = layoutNode.ToNode(VehicleTypeA, agvId, "layout-1");

        result.ShouldNotBeNull();
        result.AgvActions.Count.ShouldBe(1);
        var action = result.AgvActions.Single();
        action.ActionType.ShouldBe("pick");
        action.AgvCorrelationId.ShouldBe(agvId);
        action.BlockingType.ShouldBe(BlockingType.HARD);
        action.Theta.ShouldBe(1.5);
    }

    [TestMethod]
    public void ToEdge_VehicleTypeNotPresent_ReturnsNull()
    {
        var layoutEdge = GraphLayoutGenerator.GenerateEdge("e1", "n1", "n2", vehicleTypeIds: VehicleTypeB);

        var result = layoutEdge.ToEdge(VehicleTypeA, Guid.NewGuid());

        result.ShouldBeNull();
    }

    [TestMethod]
    public void ToEdge_VehicleTypePresent_MapsProperties()
    {
        var agvId = Guid.NewGuid();
        var layoutEdge = new GraphLayoutEdge
        {
            Id = Guid.NewGuid(),
            EdgeId = "e1",
            StartNodeId = "n1",
            EndNodeId = "n2",
            VehicleTypeProperties =
            [
                new EdgeVehicleTypeProperty
                {
                    VehicleTypeId = VehicleTypeA,
                    VehicleOrientation = 1.0,
                    OrientationType = OrientationType.Tangential,
                    RotationAllowed = false,
                    MaxSpeed = 2.5
                }
            ]
        };

        var result = layoutEdge.ToEdge(VehicleTypeA, agvId);

        result.ShouldNotBeNull();
        result.EdgeId.ShouldBe("e1");
        result.AgvOrientation.ShouldBe(1.0);
        result.OrientationType.ShouldBe(OrientationType.Tangential);
        result.RotationAllowed.ShouldBe(false);
        result.MaxSpeed.ShouldBe(2.5);
    }

    [TestMethod]
    public void ToAgvGraph_EdgeWithoutTrajectory_GetsFallbackFromNodes()
    {
        var agvId = Guid.NewGuid();
        var node1 = GraphLayoutGenerator.GenerateNode("n1", x: 1.0, y: 2.0, vehicleTypeIds: VehicleTypeA);
        var node2 = GraphLayoutGenerator.GenerateNode("n2", x: 3.0, y: 4.0, vehicleTypeIds: VehicleTypeA);
        var edge = new GraphLayoutEdge
        {
            Id = Guid.NewGuid(),
            EdgeId = "e1",
            StartNodeId = "n1",
            EndNodeId = "n2",
            VehicleTypeProperties =
            [
                new EdgeVehicleTypeProperty
                {
                    VehicleTypeId = VehicleTypeA,
                    Trajectory = new Trajectory()
                }
            ]
        };
        ICollection<GraphLayout> layouts = [GraphLayoutGenerator.GenerateLayout(nodes: [node1, node2], edges: [edge])];

        var graph = layouts.ToAgvGraph(VehicleTypeA, agvId);

        var resultEdge = graph.Edges.Single();
        resultEdge.AgvTrajectory.ControlPoints.Count.ShouldBe(2);
        resultEdge.AgvTrajectory.Degree.ShouldBe(1);
        resultEdge.AgvTrajectory.ControlPoints.First().X.ShouldBe(1.0);
        resultEdge.AgvTrajectory.ControlPoints.First().Y.ShouldBe(2.0);
        resultEdge.AgvTrajectory.ControlPoints.Last().X.ShouldBe(3.0);
        resultEdge.AgvTrajectory.ControlPoints.Last().Y.ShouldBe(4.0);
    }
}