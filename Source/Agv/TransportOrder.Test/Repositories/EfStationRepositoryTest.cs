using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        List<EfNode> nodes = graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint).Select(n => mapper.Map<EfNode>(n)).ToList();
        const string expectedMapName = "map";
        await fixture.AddMap(expectedMapName, [agvId], nodes);

        await fixture.Repository.AddStations(graph);

        foreach (var stationNode in graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint))
        {
            var station = await fixture.Repository.GetStation(stationNode.Id);
            station.MapName.ShouldBe(expectedMapName);
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
        List<EfNode> nodes = graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint).Select(n => mapper.Map<EfNode>(n)).Skip(1).ToList();
        const string expectedMapName = "map";
        await fixture.AddMap(expectedMapName, [agvId], nodes);

        await fixture.Repository.AddStations(graph);

        foreach (var stationNode in graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint))
        {
            var station = await fixture.Repository.GetStation(stationNode.Id);
            station.MapName.ShouldBeNull();
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
        List<EfNode> nodes = graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint).Select(n => mapper.Map<EfNode>(n)).ToList();
        const string expectedMapName = "map";
        await fixture.AddMap(expectedMapName, [agvId], nodes);

        await fixture.Repository.AddStations(graph);

        ICollection<Station> stations = await fixture.Repository.GetStations();
        foreach (var stationNode in graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint))
        {
            var station = stations.Single(s => s.Name == stationNode.Id);
            station.MapName.ShouldBe(expectedMapName);
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
        var fixture = new EfStationRepositoryTestFixture();
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph(agvId);

        var nodeLock = new NodeLock(graph.Nodes.First(n => n.Type != AgvGraph.NodeType.WayPoint).Id, NodeLockState.Occupied, false, agvId, DateTime.UtcNow);
        await fixture.AddAsync(nodeLock);

        string destination = graph.Nodes.First(n => n.Type != AgvGraph.NodeType.WayPoint).Id;
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(source: "source", destination: destination, assignedAgv: agvId);
        actionGroup.StartAction(actionGroup.Actions.First(a => a.Target.NodeId == destination).CorrelationId, [], []);
        await fixture.AddAsync(actionGroup);

        await fixture.Repository.AddStations(graph);

        foreach (var stationNode in graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint))
        {
            var station = await fixture.Repository.GetStation(stationNode.Id);
            station.MapName.ShouldBe(default);
            station.Length.ShouldBe(Padding);
            station.Width.ShouldBe(Padding);
            station.AdditionalTypes.ShouldBeEmpty();
            station.PrimaryType.ShouldBe(stationNode.Type);
            station.LockReason.ShouldBeNull();
            station.LockedBy.ShouldBeNull();
            if (station.Name == nodeLock.NodeId)
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
    public async Task GetStations_ValidData_GetsStationsWithNodeLockAndAgvs()
    {
        var fixture = new EfStationRepositoryTestFixture();
        var agvId = Guid.NewGuid();
        var graph = AgvGraphGenerator.CreateDefaultGraph(agvId);

        var nodeLock = new NodeLock(graph.Nodes.First(n => n.Type != AgvGraph.NodeType.WayPoint).Id, NodeLockState.Occupied, false, agvId, DateTime.UtcNow);
        await fixture.AddAsync(nodeLock);

        string destination = graph.Nodes.First(n => n.Type != AgvGraph.NodeType.WayPoint).Id;
        var actionGroup = Helper.AgvActionGenerator.GeneratePickDropActionGroup(source: "source", destination: destination, assignedAgv: agvId);
        actionGroup.StartAction(actionGroup.Actions.First(a => a.Target.NodeId == destination).CorrelationId, [], []);
        await fixture.AddAsync(actionGroup);

        await fixture.Repository.AddStations(graph);
        
        ICollection<Station> stations = await fixture.Repository.GetStations();
        foreach (var stationNode in graph.Nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint))
        {
            var station = stations.Single(s => s.Name == stationNode.Id);
            station.MapName.ShouldBe(default);
            station.Length.ShouldBe(Padding);
            station.Width.ShouldBe(Padding);
            station.AdditionalTypes.ShouldBeEmpty();
            station.PrimaryType.ShouldBe(stationNode.Type);
            station.LockReason.ShouldBeNull();
            station.LockedBy.ShouldBeNull();
            if (station.Name == nodeLock.NodeId)
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