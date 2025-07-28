using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfNodeRepositoryTest
{
    private static Mapper Mapper => MappingHelper.GetMapper(new GraphProfile());

    [TestMethod]
    public async Task GetNodeStatesForMapAsync_UnknownMap_ShouldBeEmpty()
    {
        await using var fixture = new EfNodeRepositoryTestFixture();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        List<NodeLock> nodeLocks = agvGraph.Nodes.Select(n => new NodeLock(n.Id, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow)).ToList();
        fixture.AddGraph(Mapper.Map<EfAgvGraph>(agvGraph));
        fixture.AddNodeLocks(nodeLocks);

        (await fixture.Repository.GetNodeStatesForMapAsync("unknown".ToGuid())).IsNullOrEmpty().ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNodeStatesForMapAsync_ValidData_ReturnsNodeLocks()
    {
        var mapCorrelationId = Guid.NewGuid();
        await using var fixture = new EfNodeRepositoryTestFixture();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        List<NodeLock> nodeLocks = agvGraph.Nodes.Select(n => new NodeLock(n.Id, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow)).ToList();
        fixture.AddGraph(Mapper.Map<EfAgvGraph>(agvGraph));
        fixture.AddMap(MapGenerator.Generate(mapCorrelationId, nodes: Mapper.Map<ICollection<EfNode>>(agvGraph.Nodes)));
        fixture.AddNodeLocks(nodeLocks);

        List<NodeLock> actualNodeStates = (await fixture.Repository.GetNodeStatesForMapAsync(mapCorrelationId)).Select(x => x.NodeLock).ToList();

        actualNodeStates.ShouldBe(nodeLocks, ignoreOrder: true);
    }

    [TestMethod]
    public async Task GetNodeStatesForMapAsync_NotAllNodesHaveLockState_ReturnsNodeStatesForAllNodes()
    {
        var mapCorrelationId = Guid.NewGuid();
        await using var fixture = new EfNodeRepositoryTestFixture();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        List<NodeLock> nodeLocks = agvGraph.Nodes.Take(1).Select(n => new NodeLock(n.Id, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow)).ToList();
        fixture.AddGraph(Mapper.Map<EfAgvGraph>(agvGraph));
        fixture.AddMap(MapGenerator.Generate(mapCorrelationId, nodes: Mapper.Map<ICollection<EfNode>>(agvGraph.Nodes)));
        fixture.AddNodeLocks(nodeLocks);

        List<Station> stations = agvGraph.Nodes.Take(5).Select(n => new Station(n.Id, AgvGraph.NodeType.PickDrop, new List<AgvGraph.NodeType>())).ToList();
        fixture.AddStations(stations);

        ICollection<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser)> actualNodeStates = await fixture.Repository.GetNodeStatesForMapAsync(mapCorrelationId);
        foreach (var agvGraphNode in agvGraph.Nodes)
        {
            (NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser) actualNodeState = actualNodeStates.Single(ns => ns.NodeLock.NodeId == agvGraphNode.Id);
            if (nodeLocks.Select(nl => nl.NodeId).Contains(agvGraphNode.Id))
            {
                actualNodeState.NodeLock.ShouldBe(nodeLocks.Single(nl => nl.NodeId == actualNodeState.NodeLock.NodeId));
                continue;
            }
            actualNodeState.NodeLock.IsLocked.ShouldBeFalse();
            actualNodeState.NodeLock.NodeState.ShouldBe(NodeLockState.Free);
            actualNodeState.NodeLock.AgvCorrelationId.ShouldBeNull();
        }
    }
}