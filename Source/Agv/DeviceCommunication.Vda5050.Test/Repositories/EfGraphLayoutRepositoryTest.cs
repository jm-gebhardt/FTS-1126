using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

[TestClass]
public class EfGraphLayoutRepositoryTest
{
    [TestMethod]
    public async Task StoreGraphLayoutsAsync_NewLayout_InsertsLayout()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var layout = GraphLayoutGenerator.GenerateLayout(nodes: [GraphLayoutGenerator.GenerateNode("n1"), GraphLayoutGenerator.GenerateNode("n2")]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout]);

        var stored = await fixture.Repository.GetLatestLayoutAsync("layout-1");
        stored.ShouldNotBeNull();
        stored.Nodes.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task StoreGraphLayoutsAsync_SameLayoutIdVersion_MergesNewNodesWithoutDeletingExisting()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var nodeId1 = Guid.NewGuid();
        var node1 = GraphLayoutGenerator.GenerateNode("n1", x: 1.0, y: 2.0, id: nodeId1);
        var layout = GraphLayoutGenerator.GenerateLayout(nodes: [node1]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout]);

        var nodeId2 = Guid.NewGuid();
        var node2 = GraphLayoutGenerator.GenerateNode("n2", x: 3.0, y: 4.0, id: nodeId2);
        var updatedLayout = GraphLayoutGenerator.GenerateLayout(nodes: [node2]);
        await fixture.Repository.StoreGraphLayoutsAsync([updatedLayout]);

        var stored = await fixture.Repository.GetLatestLayoutAsync("layout-1");
        stored.ShouldNotBeNull();
        stored.Nodes.Count.ShouldBe(2);
        stored.Nodes.ShouldContain(n => n.NodeId == "n1");
        stored.Nodes.ShouldContain(n => n.NodeId == "n2");
    }

    [TestMethod]
    public async Task StoreGraphLayoutsAsync_ExistingNode_OverwritesProperties()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var nodeId = Guid.NewGuid();
        var node = GraphLayoutGenerator.GenerateNode("n1", x: 1.0, y: 2.0, id: nodeId);
        var layout = GraphLayoutGenerator.GenerateLayout(nodes: [node]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout]);

        var updatedNode = GraphLayoutGenerator.GenerateNode("n1", x: 10.0, y: 20.0, id: nodeId);
        var updatedLayout = GraphLayoutGenerator.GenerateLayout(nodes: [updatedNode]);
        await fixture.Repository.StoreGraphLayoutsAsync([updatedLayout]);

        var stored = await fixture.Repository.GetLatestLayoutAsync("layout-1");
        stored.ShouldNotBeNull();
        stored.Nodes.Count.ShouldBe(1);
        var storedNode = stored.Nodes.Single();
        storedNode.PositionX.ShouldBe(10.0);
        storedNode.PositionY.ShouldBe(20.0);
    }

    [TestMethod]
    public async Task StoreGraphLayoutsAsync_SameLayoutIdVersion_MergesEdgesWithoutDeleting()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var edgeId1 = Guid.NewGuid();
        var edge1 = GraphLayoutGenerator.GenerateEdge("e1", "n1", "n2", id: edgeId1);
        var layout = GraphLayoutGenerator.GenerateLayout(edges: [edge1]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout]);

        var edgeId2 = Guid.NewGuid();
        var edge2 = GraphLayoutGenerator.GenerateEdge("e2", "n2", "n3", id: edgeId2);
        var updatedLayout = GraphLayoutGenerator.GenerateLayout(edges: [edge2]);
        await fixture.Repository.StoreGraphLayoutsAsync([updatedLayout]);

        var stored = await fixture.Repository.GetLatestLayoutAsync("layout-1");
        stored.ShouldNotBeNull();
        stored.Edges.Count.ShouldBe(2);
        stored.Edges.ShouldContain(e => e.EdgeId == "e1");
        stored.Edges.ShouldContain(e => e.EdgeId == "e2");
    }

    [TestMethod]
    public async Task StoreGraphLayoutsAsync_SameLayoutIdVersion_MergesStationsWithoutDeleting()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var station1 = GraphLayoutGenerator.GenerateStation("s1");
        var layout = GraphLayoutGenerator.GenerateLayout(stations: [station1]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout]);

        var station2 = GraphLayoutGenerator.GenerateStation("s2");
        var updatedLayout = GraphLayoutGenerator.GenerateLayout(stations: [station2]);
        await fixture.Repository.StoreGraphLayoutsAsync([updatedLayout]);

        var stored = await fixture.Repository.GetLatestLayoutAsync("layout-1");
        stored.ShouldNotBeNull();
        stored.Stations.Count.ShouldBe(2);
        stored.Stations.ShouldContain(s => s.StationId == "s1");
        stored.Stations.ShouldContain(s => s.StationId == "s2");
    }

    [TestMethod]
    public async Task StoreGraphLayoutsAsync_ExistingStation_OverwritesProperties()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var station = new GraphLayoutStation
        {
            StationId = "s1",
            StationName = "Original",
            InteractionNodeIds = ["n1"]
        };
        var layout = GraphLayoutGenerator.GenerateLayout(stations: [station]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout]);

        var updatedStation = new GraphLayoutStation
        {
            StationId = "s1",
            StationName = "Updated",
            InteractionNodeIds = ["n1", "n2"]
        };
        var updatedLayout = GraphLayoutGenerator.GenerateLayout(stations: [updatedStation]);
        await fixture.Repository.StoreGraphLayoutsAsync([updatedLayout]);

        var stored = await fixture.Repository.GetLatestLayoutAsync("layout-1");
        stored.ShouldNotBeNull();
        stored.Stations.Count.ShouldBe(1);
        stored.Stations.Single().StationName.ShouldBe("Updated");
        stored.Stations.Single().InteractionNodeIds.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task StoreGraphLayoutsAsync_DifferentLayoutVersion_InsertsNewLayout()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var layout1 = GraphLayoutGenerator.GenerateLayout(layoutVersion: "1.0", nodes: [GraphLayoutGenerator.GenerateNode("n1")]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout1]);

        var layout2 = GraphLayoutGenerator.GenerateLayout(layoutVersion: "2.0", nodes: [GraphLayoutGenerator.GenerateNode("n2")]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout2]);

        ICollection<GraphLayout> allLayouts = await fixture.Repository.GetAllLatestLayoutsAsync();
        allLayouts.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetAllLatestLayoutsAsync_MultipleLayoutIds_ReturnsLatestPerLayoutId()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var layout1 = GraphLayoutGenerator.GenerateLayout(layoutId: "L1", layoutVersion: "1.0",
            storedAt: DateTime.UtcNow.AddMinutes(-10));
        var layout2 = GraphLayoutGenerator.GenerateLayout(layoutId: "L2", layoutVersion: "1.0",
            storedAt: DateTime.UtcNow);
        await fixture.Repository.StoreGraphLayoutsAsync([layout1, layout2]);

        ICollection<GraphLayout> result = await fixture.Repository.GetAllLatestLayoutsAsync();
        result.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task HasVehicleTypeAsync_ExistingVehicleType_ReturnsTrue()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var node = GraphLayoutGenerator.GenerateNode("n1", x: 1, y: 2, vehicleTypeIds: "Maker.Series1");
        var layout = GraphLayoutGenerator.GenerateLayout(nodes: [node]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout]);

        bool result = await fixture.Repository.HasVehicleTypeAsync("Maker.Series1");
        result.ShouldBeTrue();
    }

    [TestMethod]
    public async Task HasVehicleTypeAsync_NonExistingVehicleType_ReturnsFalse()
    {
        var fixture = new GraphLayoutRepositoryTestFixture();

        var layout = GraphLayoutGenerator.GenerateLayout(nodes: [GraphLayoutGenerator.GenerateNode("n1")]);
        await fixture.Repository.StoreGraphLayoutsAsync([layout]);

        bool result = await fixture.Repository.HasVehicleTypeAsync("Maker.Unknown");
        result.ShouldBeFalse();
    }
}