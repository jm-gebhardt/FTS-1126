using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Moq;
using Shouldly;
using LifTrajectory = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif.Trajectory;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class LifMappingExtensionsTest
{
    [TestMethod]
    public void GetVehicleTypeIds_EmptyLayouts_ReturnsEmpty()
    {
        var collection = new LifLayoutCollection([], MakeMetaInfo());

        collection.GetVehicleTypeIds().ShouldBeEmpty();
    }

    [TestMethod]
    public void GetVehicleTypeIds_NodeWithVehicleType_ReturnsId()
    {
        var collection = new LifLayoutCollection([MakeLayout([MakeNode("N1", vehicleTypeId: "TYPE_A")], [])], MakeMetaInfo());

        collection.GetVehicleTypeIds().ShouldHaveSingleItem().ShouldBe("TYPE_A");
    }

    [TestMethod]
    public void GetVehicleTypeIds_EdgeWithVehicleType_ReturnsId()
    {
        var collection = new LifLayoutCollection([MakeLayout([], [MakeEdge("E1", "N1", "N2", vehicleTypeId: "TYPE_B")])], MakeMetaInfo());

        collection.GetVehicleTypeIds().ShouldHaveSingleItem().ShouldBe("TYPE_B");
    }

    [TestMethod]
    public void GetVehicleTypeIds_SameTypeOnNodeAndEdge_ReturnsDistinct()
    {
        var node = MakeNode("N1", vehicleTypeId: "TYPE_A");
        var edge = MakeEdge("E1", "N1", "N2", vehicleTypeId: "TYPE_A");
        var collection = new LifLayoutCollection([MakeLayout([node], [edge])], MakeMetaInfo());

        collection.GetVehicleTypeIds().ShouldHaveSingleItem().ShouldBe("TYPE_A");
    }

    [TestMethod]
    public void GetVehicleTypeIds_MultipleDistinctTypes_ReturnsAll()
    {
        var node = MakeNode("N1", vehicleTypeId: "TYPE_A");
        var edge = MakeEdge("E1", "N1", "N2", vehicleTypeId: "TYPE_B");
        var collection = new LifLayoutCollection([MakeLayout([node], [edge])], MakeMetaInfo());

        HashSet<string> result = collection.GetVehicleTypeIds();

        result.Count.ShouldBe(2);
        result.ShouldContain("TYPE_A");
        result.ShouldContain("TYPE_B");
    }

    [TestMethod]
    public void ToNurbs_NullTrajectory_ReturnsNull()
    {
        LifTrajectory? trajectory = null;

        trajectory.ToNurbs().ShouldBeNull();
    }

    [TestMethod]
    public void ToNurbs_EmptyControlPoints_ReturnsNull()
    {
        var trajectory = new LifTrajectory();

        trajectory.ToNurbs().ShouldBeNull();
    }

    [TestMethod]
    public void ToNurbs_WithControlPoints_MapsProperties()
    {
        var trajectory = new LifTrajectory(
            controlPoints: [new ControlPoint(1.0, 2.0, 0.5), new ControlPoint(3.0, 4.0)],
            knotVector: [0.0, 0.5, 1.0],
            degree: 2);

        var result = trajectory.ToNurbs();

        result.ShouldNotBeNull();
        result.Degree.ShouldBe(2);
        result.KnotVector.ShouldBe([0.0, 0.5, 1.0]);
        result.ControlPoints.Length.ShouldBe(2);
        result.ControlPoints[0].X.ShouldBe(1.0);
        result.ControlPoints[0].Y.ShouldBe(2.0);
        result.ControlPoints[0].Weight.ShouldBe(0.5);
    }

    [TestMethod]
    public void ToNurbs_NullDegree_DefaultsToOne()
    {
        var trajectory = new LifTrajectory([new ControlPoint(0, 0)], [], degree: null);

        trajectory.ToNurbs()!.Degree.ShouldBe(1);
    }

    [TestMethod]
    public void NodeParameters_ToOrderParameters_Null_ReturnsEmpty()
    {
        ICollection<NodeActionParameter>? parameters = null;

        parameters.ToOrderParameters().ShouldBeEmpty();
    }

    [TestMethod]
    public void NodeParameters_ToOrderParameters_Empty_ReturnsEmpty()
    {
        ICollection<NodeActionParameter> parameters = [];

        parameters.ToOrderParameters().ShouldBeEmpty();
    }

    [TestMethod]
    public void NodeParameters_ToOrderParameters_WithItems_MapsKeyAndValue()
    {
        ICollection<NodeActionParameter> parameters =
        [
            new ("key1", "val1"),
            new ("key2", "val2")
        ];

        ICollection<Vda5050.Domain.Models.Orders.OrderActionParameter> result = parameters.ToOrderParameters();

        result.Count.ShouldBe(2);
        result.ShouldContain(p => p.Key == "key1" && (string)p.Value == "val1");
        result.ShouldContain(p => p.Key == "key2" && (string)p.Value == "val2");
    }

    [TestMethod]
    public void EdgeParameters_ToOrderParameters_Null_ReturnsEmpty()
    {
        ICollection<EdgeActionParameter>? parameters = null;

        parameters.ToOrderParameters().ShouldBeEmpty();
    }

    [TestMethod]
    public void EdgeParameters_ToOrderParameters_Empty_ReturnsEmpty()
    {
        ICollection<EdgeActionParameter> parameters = [];

        parameters.ToOrderParameters().ShouldBeEmpty();
    }

    [TestMethod]
    public void EdgeParameters_ToOrderParameters_WithItems_MapsKeyAndValue()
    {
        ICollection<EdgeActionParameter> parameters = [new ("edgeKey", "edgeVal")];

        ICollection<Vda5050.Domain.Models.Orders.OrderActionParameter> result = parameters.ToOrderParameters();

        result.ShouldHaveSingleItem();
        result.Single().Key.ShouldBe("edgeKey");
        ((string)result.Single().Value).ShouldBe("edgeVal");
    }

    [TestMethod]
    public void ToAgvGraph_EmptyLayouts_ReturnsEmptyGraph()
    {
        ICollection<Layout> layouts = [];

        var result = layouts.ToAgvGraph(Guid.NewGuid(), "MY_TYPE", new Mock<INurbsEdgeBuilder>().Object);

        result.Nodes.ShouldBeEmpty();
        result.Edges.ShouldBeEmpty();
    }

    [TestMethod]
    public void ToAgvGraph_NodeWithNoMatchingVehicleType_IsExcluded()
    {
        ICollection<Layout> layouts = [MakeLayout([MakeNode("N1", vehicleTypeId: "OTHER")], [])];

        var result = layouts.ToAgvGraph(Guid.NewGuid(), "MY_TYPE", new Mock<INurbsEdgeBuilder>().Object);

        result.Nodes.ShouldBeEmpty();
    }

    [TestMethod]
    public void ToAgvGraph_NodeWithMatchingVehicleType_IncludesNodeCoordinates()
    {
        ICollection<Layout> layouts = [MakeLayout([MakeNode("N1", x: 1.5, y: 2.5, vehicleTypeId: "MY_TYPE")], [])];

        var result = layouts.ToAgvGraph(Guid.NewGuid(), "MY_TYPE", new Mock<INurbsEdgeBuilder>().Object);

        var node = result.Nodes.ShouldHaveSingleItem();
        node.X.ShouldBe(1.5f);
        node.Y.ShouldBe(2.5f);
    }

    [TestMethod]
    public void ToAgvGraph_DirectEdgeBetweenMatchingNodes_IsIncluded()
    {
        var node1 = MakeNode("N1", x: 0, y: 0, vehicleTypeId: "MY_TYPE");
        var node2 = MakeNode("N2", x: 3, y: 4, vehicleTypeId: "MY_TYPE");
        var edge = MakeEdge("E1", "N1", "N2", vehicleTypeId: "MY_TYPE");
        ICollection<Layout> layouts = [MakeLayout([node1, node2], [edge])];

        var result = layouts.ToAgvGraph(Guid.NewGuid(), "MY_TYPE", new Mock<INurbsEdgeBuilder>().Object);

        result.Edges.ShouldHaveSingleItem();
    }

    private static MetaInformation MakeMetaInfo() =>
        new("tester", DateTimeOffset.UtcNow, "1.0", "proj");

    private static Layout MakeLayout(ICollection<Node> nodes, ICollection<Edge> edges) =>
        new(Guid.NewGuid(), "layout-1", "1.0", edges, nodes, []);

    private static Node MakeNode(string nodeId, double x = 0, double y = 0, string vehicleTypeId = "DEFAULT") =>
        new(nodeId, new NodePosition(x, y), [new VehicleTypeNodeProperty(Guid.NewGuid(), vehicleTypeId)]);

    private static Edge MakeEdge(string edgeId, string startNodeId, string endNodeId, string vehicleTypeId = "DEFAULT") =>
        new(edgeId, endNodeId, startNodeId, [new VehicleTypeEdgeProperty(Guid.NewGuid(), vehicleTypeId)]);
}
