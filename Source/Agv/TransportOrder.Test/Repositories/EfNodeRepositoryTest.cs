using AutoMapper;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
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
    public async Task GetNodeStatesForMapAsync_UnknownMap_ThrowsMapNotFoundException()
    {
        await using var fixture = new EfNodeRepositoryTestFixture();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        List<NodeLock> nodeLocks = agvGraph.Nodes.Select(n => new NodeLock(n.Id, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow)).ToList();
        fixture.AddGraph(Mapper.Map<EfAgvGraph>(agvGraph));
        fixture.AddNodeLocks(nodeLocks);

        await Should.ThrowAsync<MapNotFoundException>(fixture.Repository.GetNodeStatesForMapAsync("unknown".ToGuid()));
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

        (await fixture.Repository.GetNodeStatesForMapAsync(mapCorrelationId)).ToList().ShouldBe(nodeLocks, ignoreOrder: true);
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

        ICollection<NodeLock> actualNodeStates = await fixture.Repository.GetNodeStatesForMapAsync(mapCorrelationId);
        foreach (var agvGraphNode in agvGraph.Nodes)
        {
            var actualNodeState = actualNodeStates.Single(ns => ns.NodeId == agvGraphNode.Id);
            if (nodeLocks.Select(nl => nl.NodeId).Contains(agvGraphNode.Id))
            {
                actualNodeState.ShouldBe(nodeLocks.Single(nl => nl.NodeId == actualNodeState.NodeId));
                continue;
            }
            actualNodeState.IsLocked.ShouldBeFalse();
            actualNodeState.NodeState.ShouldBe(NodeLockState.Free);
            actualNodeState.AgvCorrelationId.ShouldBeNull();
        }
    }
}