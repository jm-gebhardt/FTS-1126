using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfStationRepositoryTest
{
    private const double Padding = 0.2;

    [TestMethod]
    public async Task AddStations_GetStation_AgvDataAndMapsExist_AddsAndGetsStationsWithSizeAndMap()
    {
        var fixture = new EfStationRepositoryTestFixture();
        List<AgvData> specifications = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        double expectedMaxWidth = specifications.Max(s => s.Width) + Padding;
        double expectedMaxLength = specifications.Max(s => s.Length) + Padding;
        await fixture.AddRangeAsync(specifications);
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph(agvId);

        var mapper = MappingHelper.GetMapper(new GraphProfile());
        List<GraphNode> nodes = graph.Nodes.Where(n => n.IsStation()).Select(n => mapper.Map<GraphNode>(n)).ToList();
        const string mapName = "map";
        var mapId = mapName.ToGuid();
        await fixture.AddMap(mapId, mapName, [agvId], nodes);

        await fixture.Repository.AddStations(graph);

        foreach (var stationNode in graph.Nodes.Where(n => n.IsStation()))
        {
            var station = await fixture.Repository.GetStation(stationNode.Id);
            station.MapId.ShouldBe(mapId);
            station.Length.ShouldBe(expectedMaxLength);
            station.Width.ShouldBe(expectedMaxWidth);
            station.NodeLock.ShouldBeNull();
            station.AdditionalTypes.ShouldBeEmpty();
            station.PrimaryType.ShouldBe(stationNode.Type);
            station.Agvs.ShouldBeEmpty();
            station.LockReason.ShouldBeNull();
            station.LockedBy.ShouldBeNull();
        }
    }

    [TestMethod]
    public async Task AddStations_GetStation_StationWithAdditionalType_AddsAndGetsStations()
    {
        var fixture = new EfStationRepositoryTestFixture();
        List<AgvData> specifications = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        double expectedMaxWidth = specifications.Max(s => s.Width) + Padding;
        double expectedMaxLength = specifications.Max(s => s.Length) + Padding;
        await fixture.AddRangeAsync(specifications);
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateGraphWithAdditionalStationType(agvId);
        var mapper = MappingHelper.GetMapper(new GraphProfile());
        List<GraphNode> nodes = graph.Nodes.Where(n => n.IsStation()).Select(n => mapper.Map<GraphNode>(n)).ToList();
        const string mapName = "map";
        var mapId = mapName.ToGuid();
        await fixture.AddMap(mapId, mapName, [agvId], nodes);

        await fixture.Repository.AddStations(graph);

        foreach (var stationNode in graph.Nodes.Where(n => n.IsStation()))
        {
            var station = await fixture.Repository.GetStation(stationNode.Id);
            station.MapId.ShouldBe(mapId);
            station.Length.ShouldBe(expectedMaxLength);
            station.Width.ShouldBe(expectedMaxWidth);
            station.NodeLock.ShouldBeNull();
            station.PrimaryType.ShouldBe(stationNode.GetPrimaryType());
            station.AdditionalTypes.ShouldBe(stationNode.GetAdditionalTypes());
            station.Agvs.ShouldBeEmpty();
            station.LockReason.ShouldBeNull();
            station.LockedBy.ShouldBeNull();
        }
    }

    [TestMethod]
    public async Task AddStations_GetStation_NotAllStationsOnMap_AddsAndGetsStationsWithoutMap()
    {
        var fixture = new EfStationRepositoryTestFixture();
        List<AgvData> specifications = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        double expectedMaxWidth = specifications.Max(s => s.Width) + Padding;
        double expectedMaxLength = specifications.Max(s => s.Length) + Padding;
        await fixture.AddRangeAsync(specifications);
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph(agvId);

        var mapper = MappingHelper.GetMapper(new GraphProfile());
        List<GraphNode> nodes = graph.Nodes.Where(n => n.IsStation()).Select(n => mapper.Map<GraphNode>(n)).OrderBy(n => n.Name).Skip(1).ToList();
        const string mapName = "map";
        var mapId = mapName.ToGuid();
        await fixture.AddMap(mapId, mapName, [agvId], nodes);

        await fixture.Repository.AddStations(graph);

        foreach (var stationNode in graph.Nodes.Where(n => n.IsStation()))
        {
            var station = await fixture.Repository.GetStation(stationNode.Id);
            station.MapId.ShouldBeNull();
            station.Length.ShouldBe(expectedMaxLength);
            station.Width.ShouldBe(expectedMaxWidth);
            station.NodeLock.ShouldBeNull();
            station.AdditionalTypes.ShouldBeEmpty();
            station.PrimaryType.ShouldBe(stationNode.Type);
            station.Agvs.ShouldBeEmpty();
            station.LockReason.ShouldBeNull();
            station.LockedBy.ShouldBeNull();
        }
    }

    [TestMethod]
    public async Task AddStations_GetStations_AgvDataAndMapsExist_AddsAndGetsStationsWithSizeAndMap()
    {
        var fixture = new EfStationRepositoryTestFixture();
        List<AgvData> specifications = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        double expectedMaxWidth = specifications.Max(s => s.Width) + Padding;
        double expectedMaxLength = specifications.Max(s => s.Length) + Padding;
        await fixture.AddRangeAsync(specifications);

        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph(agvId);

        var mapper = MappingHelper.GetMapper(new GraphProfile());
        List<GraphNode> nodes = graph.Nodes.Where(n => n.IsStation()).Select(n => mapper.Map<GraphNode>(n)).ToList();
        const string mapName = "map";
        var mapId = mapName.ToGuid();
        await fixture.AddMap(mapId, mapName, [agvId], nodes);

        await fixture.Repository.AddStations(graph);

        ICollection<Station> stations = await fixture.Repository.GetStations();
        foreach (var stationNode in graph.Nodes.Where(n => n.IsStation()))
        {
            var station = stations.Single(s => s.Name == stationNode.Name);
            station.MapId.ShouldBe(mapId);
            station.Length.ShouldBe(expectedMaxLength);
            station.Width.ShouldBe(expectedMaxWidth);
            station.NodeLock.ShouldBeNull();
            station.AdditionalTypes.ShouldBeEmpty();
            station.PrimaryType.ShouldBe(stationNode.Type);
            station.Agvs.ShouldBeEmpty();
            station.LockReason.ShouldBeNull();
            station.LockedBy.ShouldBeNull();
        }
    }

    [TestMethod]
    public async Task GetStation_ValidData_GetsStationWithNodeLockAndAgvs()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph(agvId);
        var nodeLock = new NodeLock(graph.Nodes.First(n => n.IsStation()), NodeLockState.Occupied, false, agvId, DateTime.UtcNow);
        var fixture = await new EfStationRepositoryTestFixture()
            .WithNodeLockAsync(nodeLock)
            .BuildAsync();

        string destination = graph.Nodes.First(n => n.IsStation()).Name;
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(source: "source", destination: destination, assignedAgv: agvId);
        actionGroup.StartAction(actionGroup.Actions.First(a => a.Target.Node!.Name == destination).CorrelationId, [], [], DateTimeOffset.Now);
        await fixture.AddActionGroupAsync(actionGroup, true);
        await fixture.Repository.AddStations(graph);

        foreach (var stationNode in graph.Nodes.Where(n => n.IsStation()))
        {
            var station = await fixture.Repository.GetStation(stationNode.Id);
            station.MapId.ShouldBeNull();
            station.Length.ShouldBe(Padding);
            station.Width.ShouldBe(Padding);
            station.AdditionalTypes.ShouldBeEmpty();
            station.PrimaryType.ShouldBe(stationNode.Type);
            station.LockReason.ShouldBeNull();
            station.LockedBy.ShouldBeNull();
            if (station.Id == nodeLock.NodeId)
            {
                station.NodeLock.ShouldBeEquivalentTo(nodeLock);
            }
            else
            {
                station.NodeLock.ShouldBeNull();
            }
            if (station.Name == destination)
            {
                station.Agvs.ShouldBe([agvId]);
                continue;
            }
            station.Agvs.ShouldBeEmpty();
        }
    }

    [TestMethod]
    public async Task GetStations_ValidData_GetsStationsWithNodeLockAndAgvs()
    {
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph(agvId);
        var nodeLock = new NodeLock(graph.Nodes.First(n => n.IsStation()), NodeLockState.Occupied, false, agvId, DateTime.UtcNow);
        var fixture = await new EfStationRepositoryTestFixture()
            .WithNodeLockAsync(nodeLock)
            .BuildAsync();

        string destination = graph.Nodes.First(n => n.IsStation()).Name;
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(source: "source", destination: destination, assignedAgv: agvId);
        actionGroup.StartAction(actionGroup.Actions.First(a => a.Target.Node!.Name == destination).CorrelationId, [], [], DateTimeOffset.Now);
        await fixture.AddActionGroupAsync(actionGroup, true);

        await fixture.Repository.AddStations(graph);

        ICollection<Station> stations = await fixture.Repository.GetStations();
        foreach (var stationNode in graph.Nodes.Where(n => n.IsStation()))
        {
            var station = stations.Single(s => s.Name == stationNode.Name);
            station.MapId.ShouldBeNull();
            station.Length.ShouldBe(Padding);
            station.Width.ShouldBe(Padding);
            station.AdditionalTypes.ShouldBeEmpty();
            station.PrimaryType.ShouldBe(stationNode.Type);
            station.LockReason.ShouldBeNull();
            station.LockedBy.ShouldBeNull();
            if (station.Id == nodeLock.NodeId)
            {
                station.NodeLock.ShouldBe(nodeLock);
            }
            else
            {
                station.NodeLock.ShouldBeNull();
            }
            if (station.Name == destination)
            {
                station.Agvs.ShouldBe([agvId]);
                continue;
            }
            station.Agvs.ShouldBeEmpty();
        }
    }

    [TestMethod]
    public async Task Update_ValidData_StationUpdated()
    {
        var station = new Station("name", AgvGraph.NodeType.PickDrop, []);
        var fixture = new EfStationRepositoryTestFixture();
        await fixture.AddAsync(station);

        station.Lock("user", "reason", "description", LockedOrderHandlingType.Abort);

        await fixture.Repository.Update(station);

        var updatedStation = await fixture.Repository.GetStation(station.Id);

        updatedStation.IsLocked.ShouldBe(station.IsLocked);
    }
}