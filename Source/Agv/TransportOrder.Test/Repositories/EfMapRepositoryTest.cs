using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfMapRepositoryTest
{
    [TestMethod]
    public async Task GetAllMaps_ReturnsEmpty_List_WhenRepositoryIsEmpty()
    {
        var fixture = new EfMapRepositoryTestFixture();

        ICollection<Map> maps = await fixture.Repository.GetAllMapsAsync();
        maps.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAllMaps_ShouldReturnListOfMaps()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map1 = MapGenerator.Generate();
        var map2 = MapGenerator.Generate();

        fixture.Add(map1);
        fixture.Add(map2);

        ICollection<Map> maps = await fixture.Repository.GetAllMapsAsync();
        maps.ShouldNotBeEmpty();
        maps.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task GetBackgroundImageAsync_ShouldReturnMapImage_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map1 = MapGenerator.Generate();

        fixture.Add(map1);

        byte[] backgroundImage = await fixture.Repository.GetBackgroundImageAsync(map1.CorrelationId);

        backgroundImage.ShouldBe(map1.BaseBackground!.Image);
    }

    [TestMethod]
    public async Task GetMapAsync_ShouldContainMapAgv_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = MapGenerator.Generate();
        map.MapAgvs.Add(new MapAgv(map.CorrelationId, map, Guid.NewGuid(), null));
        fixture.Add(map);

        var actualMap = await fixture.Repository.GetMapAsync(map.CorrelationId);

        actualMap.MapAgvs.Count.ShouldBe(1);
        actualMap.MapAgvs.Single().MapId.ShouldBe(map.CorrelationId);
        actualMap.MapAgvs.Single().AgvId.ShouldBe(actualMap.MapAgvs.Single().AgvId);
    }

    [TestMethod]
    public void Exists_ShouldReturnTrue_WhenMapIsFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = MapGenerator.Generate();

        fixture.Add(map);

        bool found = fixture.Repository.Exists(map.CorrelationId);
        found.ShouldBeTrue();
    }

    [TestMethod]
    public void Exists_ShouldReturnFalse_WhenMapIsNotFound()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = MapGenerator.Generate();

        fixture.Add(map);

        bool found = fixture.Repository.Exists(Guid.NewGuid());
        found.ShouldBeFalse();
    }

    [TestMethod]
    public async Task AddOrUpdate_ShouldReturnTrue_WhenAddingNewMap()
    {
        var fixture = new EfMapRepositoryTestFixture();
        var map = MapGenerator.Generate();

        bool result = await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);
        result.ShouldBeTrue();

        var background = await fixture.Repository.GetMapAsync(map.CorrelationId);
        background.BaseBackground.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task AddOrUpdate_WithNodesAndEdges_AddsMapWithNodesAndEdges()
    {
        var fixture = new EfMapRepositoryTestFixture();
        var n1 = fixture.Mapper.Map<EfNode>(NodeGenerator.GenerateNode(nodeId: "1"));
        var n2 = fixture.Mapper.Map<EfNode>(NodeGenerator.GenerateNode(nodeId: "2"));
        var e = fixture.Mapper.Map<EfEdge>(EdgeGenerator.Generate("1", "2"));
        var agv = AgvGenerator.GenerateIdleAgv(0, "1");

        fixture.Add(n1);
        fixture.Add(n2);
        fixture.Add(e);
        fixture.Add(agv);

        var map = MapGenerator.Generate(nodes: [n1, n2], edges: [e]);

        bool result = await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);
        result.ShouldBeTrue();
        var savedMap = (await fixture.Repository.GetAllMapsAsync()).First();
        savedMap.Edges.Count.ShouldBe(1);
        savedMap.Nodes.Count.ShouldBe(2);
    }
    
    [TestMethod]
    public async Task AssignAgvGraphToMap_WithNodesEdgesAgv_AddsGraphWithNodesAndEdges()
    {
        var fixture = new EfMapRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(0, "1");
        var graph = fixture.Mapper.Map<EfAgvGraph>( AgvGraphGenerator.CreateDefaultGraph(agv.CorrelationId));

        fixture.Add(graph);
        fixture.Add(agv);
         
        var map = MapGenerator.Generate(nodes: graph.Nodes, edges: graph.Edges);

        await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);

        await fixture.Repository.AssignAgvGraphToMapsAsync(agv.CorrelationId);
        
        var savedMap = (await fixture.Repository.GetAllMapsAsync()).First();
        savedMap.Edges.Count.ShouldBe(graph.Edges.Count);
        savedMap.Nodes.Count.ShouldBe(graph.Nodes.Count);
    }

    [TestMethod]
    public async Task UpdateMap_ExistingMap_MapUpdated()
    {
        var fixture = new EfMapRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(0, "1");
        var graph = fixture.Mapper.Map<EfAgvGraph>(AgvGraphGenerator.CreateDefaultGraph(agv.CorrelationId));

        fixture.Add(graph);
        fixture.Add(agv);

        var map = MapGenerator.Generate(nodes: graph.Nodes, edges: graph.Edges);

        await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);

        await fixture.Repository.AssignAgvGraphToMapsAsync(agv.CorrelationId);

        var mapUpdate = MapGenerator.Generate(nodes: graph.Nodes.Take(6).ToList(), edges: graph.Edges.Take(6).ToList(), correlationId: map.CorrelationId);

        await fixture.Repository.AddOrUpdateAsync(mapUpdate, CancellationToken.None);
        
        var savedMap = (await fixture.Repository.GetAllMapsAsync()).First();
        savedMap.Edges.Count.ShouldBe(6);
        savedMap.Nodes.Count.ShouldBe(6);
    }
    
    [TestMethod]
    public async Task AddOrUpdate_DifferentProperties_UpdatesAllProperties()
    {
        var fixture = new EfMapRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(0, "1");
        var otherAgv = AgvGenerator.GenerateIdleAgv(1, "3");
        var graph = fixture.Mapper.Map<EfAgvGraph>(AgvGraphGenerator.CreateDefaultGraph(agv.CorrelationId));

        fixture.Add(graph);
        fixture.Add(agv);
        fixture.Add(otherAgv);

        List<EfEdge> edges = graph.Edges.Take(2).ToList();
        List<EfNode> nodes = graph.Nodes.Take(2).ToList();

        var map = MapGenerator.Generate(nodes: nodes, edges: edges);

        map.CustomBackground = new MapBackground(Guid.NewGuid(), 1, 1, 1, 1, 1, new byte[1], true);

        await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);

        await fixture.Repository.AssignAgvGraphToMapsAsync(agv.CorrelationId);

        List<EfNode> newNodes = nodes.Select(n => fixture.Mapper.Map<EfNode>(new GraphNode(n.Id, n.X, n.Y, n.Z, AgvGraph.NodeType.Maintenance, 1337, 1337, DockingDirection.Both))).ToList();
        newNodes = newNodes.Take(1).ToList();
        var newNode = fixture.Mapper.Map<EfNode>(new GraphNode("testNode", 42, 42, 42, AgvGraph.NodeType.Maintenance, null, null, DockingDirection.Both));

        fixture.Add(newNode);
        newNodes.Add(newNode);

        var mapUpdate = MapGenerator.Generate(
            nodes: newNodes,
            edges: edges.Take(1).ToList(),
            correlationId: map.CorrelationId,
            mapAgvs: [ new MapAgv(map.CorrelationId, null, otherAgv.CorrelationId, null)]
        );

        var newCustomBackground = new MapBackground(Guid.NewGuid(), 2, 1, 1, 1, 1, new byte[1], true);
        var newBaseBackground = new MapBackground(Guid.NewGuid(), 3, 1, 1, 1, 1, new byte[1], true);

        mapUpdate.CustomBackground = newCustomBackground;
        mapUpdate.BaseBackground = newBaseBackground;

        await fixture.Repository.AddOrUpdateAsync(mapUpdate, CancellationToken.None);

        var savedMap = (await fixture.Repository.GetAllMapsAsync()).First();
        
        savedMap.Nodes.Count.ShouldBe(2);
        savedMap.Nodes.ShouldAllBe(n => n.PickLiftHeight == null && newNodes.Any(nn => nn.Id == n.Id));
        savedMap.Edges.Count.ShouldBe(1);
        savedMap.MapAgvs.Count.ShouldBe(1);
        savedMap.MapAgvs.Single().AgvId.ShouldBe(otherAgv.CorrelationId);
        savedMap.BaseBackground.ShouldNotBeNull();
        savedMap.CustomBackground.ShouldNotBeNull();
        savedMap.BaseBackground.CorrelationId.ShouldBe(mapUpdate.BaseBackground.CorrelationId);
        savedMap.CustomBackground.CorrelationId.ShouldBe(mapUpdate.CustomBackground.CorrelationId);
        savedMap.BaseBackground.ShouldBeEquivalentTo(newBaseBackground);
        savedMap.CustomBackground.ShouldBeEquivalentTo(newCustomBackground);
    }

    [TestMethod]
    public async Task AddOrUpdate_ShouldReturnTrue_WhenUpdatingMap()
    {
        var fixture = new EfMapRepositoryTestFixture();

        var map = MapGenerator.Generate();

        bool result = await fixture.Repository.AddOrUpdateAsync(map, CancellationToken.None);
        result.ShouldBeTrue();

        var background = await fixture.Repository.GetMapAsync(map.CorrelationId);
        background.BaseBackground.ShouldNotBeNull();
        background.BaseBackground.Image.ShouldBe(map.BaseBackground!.Image);

        const string mapNew = "map-new";
        const int width = 22;
        const int height = 33;
        const double resolution = 44;
        const double xOffset = 55;
        const double yOffset = 66;
        var mapUpdate = MapGenerator.Generate(
            mapNew.ToGuid(),
            Convert.FromBase64String(EfMapRepositoryTestFixture.SmallImage2),
            mapNew,
            width,
            height,
            resolution,
            xOffset,
            yOffset,
            DateTime.Now);

        result = await fixture.Repository.AddOrUpdateAsync(mapUpdate, CancellationToken.None);
        result.ShouldBeTrue();
        background = await fixture.Repository.GetMapAsync(mapUpdate.CorrelationId);
        background.BaseBackground.ShouldNotBeNull();
        background.BaseBackground.Image.ShouldNotBeNull();
        background.BaseBackground.Image.ShouldBe(Convert.FromBase64String(EfMapRepositoryTestFixture.SmallImage2));
        background.BaseBackground.Width.ShouldBe(width);
        background.BaseBackground.Height.ShouldBe(height);
        background.BaseBackground.Resolution.ShouldBe(resolution);
        background.BaseBackground.XOffset.ShouldBe(xOffset);
        background.BaseBackground.YOffset.ShouldBe(yOffset);
    }
}