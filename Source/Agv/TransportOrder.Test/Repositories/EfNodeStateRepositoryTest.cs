using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class GraphNodeStateRepositoryTest
{
    private static readonly GraphNode _node = "TestNode".ToGraphNode();

    private static Guid NodeId => _node.Id;

    [TestMethod]
    public async Task When_LockNodeAsync_RepositoryEmpty_LockSuccessful()
    {
        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNode(_node);
        (await fixture.Repository.LockNodeAsync(NodeId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeTrue();
        nodeLock.AgvCorrelationId.ShouldBeNull();
    }

    [TestMethod]
    public async Task When_LockNodeAsync_NodeExists_LockSuccessful_AllOtherParameterUnchanged()
    {
        var initialNodeLock = new NodeLock(_node, NodeLockState.Free, false, Guid.NewGuid(), DateTime.UtcNow);
        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.LockNodeAsync(NodeId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeTrue();
        nodeLock.NodeId.ShouldBe(initialNodeLock.NodeId);
        nodeLock.NodeState.ShouldBe(initialNodeLock.NodeState);
        nodeLock.AgvCorrelationId.ShouldBe(initialNodeLock.AgvCorrelationId);
        nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
    }

    [TestMethod]
    public async Task When_LockNodeAsync_NodeAlreadyLocked_LockFailed()
    {
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), NodeLockState.Free, true, Guid.NewGuid(), DateTime.UtcNow);
        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.LockNodeAsync(NodeId)).ShouldBeFalse();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeTrue();
        nodeLock.NodeId.ShouldBe(initialNodeLock.NodeId);
        nodeLock.NodeState.ShouldBe(initialNodeLock.NodeState);
        nodeLock.AgvCorrelationId.ShouldBe(initialNodeLock.AgvCorrelationId);
        nodeLock.ModifiedDate.ShouldBe(initialNodeLock.ModifiedDate);
    }

    [TestMethod]
    public async Task When_UnLockNodeAsync_NodeLocked_UnlockSuccessful()
    {
        var initialNodeLock = new NodeLock(_node, NodeLockState.Free, true, Guid.NewGuid(), DateTime.UtcNow);
        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.UnlockNodeAsync(NodeId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeFalse();
        nodeLock.NodeState.ShouldBe(initialNodeLock.NodeState);
        nodeLock.AgvCorrelationId.ShouldBe(initialNodeLock.AgvCorrelationId);
        nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
    }

    [TestMethod]
    public async Task When_UnLockNodeAsync_RepositoryEmpty_UnLockFailed()
    {
        var fixture = new GraphNodeStateRepositoryTestFixture();
        (await fixture.Repository.UnlockNodeAsync(NodeId)).ShouldBeFalse();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldBeNull();
    }

    [TestMethod]
    public async Task When_UnLockNodeAsync_NodeAlreadyUnLocked_LockFailed()
    {
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), NodeLockState.Free, false, Guid.NewGuid(), DateTime.UtcNow);
        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.UnlockNodeAsync(NodeId)).ShouldBeFalse();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.NodeId.ShouldBe(initialNodeLock.NodeId);
    }

    [TestMethod]
    public async Task When_ReserveNodeAsync_RepositoryEmpty_Successful()
    {
        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNode(_node);
        var agvCorrelationId = Guid.NewGuid();
        (await fixture.Repository.ReserveNodeAsync(NodeId, agvCorrelationId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeFalse();
        nodeLock.NodeState.ShouldBe(NodeLockState.Reserved);
        nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task When_ReserveNodeAsync_NodeExists_Successful()
    {
        var agvCorrelationId = Guid.NewGuid();
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), NodeLockState.Free, false, Guid.NewGuid(), DateTime.UtcNow);

        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.ReserveNodeAsync(NodeId, agvCorrelationId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeFalse();
        nodeLock.NodeState.ShouldBe(NodeLockState.Reserved);
        nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
        nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
    }

    [TestMethod]
    public async Task When_ReserveNodeAsync_NodeOccupied_Failed()
    {
        var agvCorrelationId = Guid.NewGuid();
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow);
        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.ReserveNodeAsync(NodeId, agvCorrelationId)).ShouldBeFalse();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.NodeId.ShouldBe(initialNodeLock.NodeId);
    }

    [TestMethod]
    public async Task When_ReserveNodeAsync_NodeLocked_Failed()
    {
        var agvCorrelationId = Guid.NewGuid();
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), NodeLockState.Free, true, null, DateTime.UtcNow);

        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.ReserveNodeAsync(NodeId, agvCorrelationId)).ShouldBeFalse();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.NodeId.ShouldBe(initialNodeLock.NodeId);
    }

    [TestMethod]
    public async Task When_UnReserveNodeAsync_NodeExists_Successful()
    {
        var agvCorrelationId = Guid.NewGuid();
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow);

        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);
        (await fixture.Repository.UnReserveNodeAsync(agvCorrelationId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeFalse();
        nodeLock.NodeState.ShouldBe(NodeLockState.Free);
        nodeLock.AgvCorrelationId.ShouldBeNull();
        nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
    }

    [DataRow(NodeLockState.Free)]
    [DataRow(NodeLockState.Occupied)]
    [TestMethod]
    public async Task When_UnReserveNodeAsync_NodeNotReserved_Failed(NodeLockState state)
    {
        var agvCorrelationId = Guid.NewGuid();
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), state, false, Guid.NewGuid(), DateTime.UtcNow);

        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.UnReserveNodeAsync(agvCorrelationId)).ShouldBeFalse();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.NodeId.ShouldBe(initialNodeLock.NodeId);
    }

    [TestMethod]
    public async Task When_OccupyNodeAsync_RepositoryEmpty_Successful()
    {
        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNode(_node);
        var agvCorrelationId = Guid.NewGuid();
        (await fixture.Repository.OccupyNodeAsync(NodeId, agvCorrelationId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeFalse();
        nodeLock.NodeState.ShouldBe(NodeLockState.Occupied);
        nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task When_OccupyNodeAsync_NodeExistsFree_Successful()
    {
        var agvCorrelationId = Guid.NewGuid();
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), NodeLockState.Free, false, Guid.NewGuid(), DateTime.UtcNow);

        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.OccupyNodeAsync(NodeId, agvCorrelationId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeFalse();
        nodeLock.NodeState.ShouldBe(NodeLockState.Occupied);
        nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
        nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
    }

    [TestMethod]
    public async Task When_OccupyNodeAsync_NodeExistsReserved_Successful()
    {
        var agvCorrelationId = Guid.NewGuid();
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);

        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.OccupyNodeAsync(NodeId, agvCorrelationId)).ShouldBeTrue();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.IsLocked.ShouldBeFalse();
        nodeLock.NodeState.ShouldBe(NodeLockState.Occupied);
        nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
        nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
    }

    [DataRow(NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved)]
    [TestMethod]
    public async Task When_UnOccupyNodeAsync_NodeNotOccupied_Failed(NodeLockState state)
    {
        var agvCorrelationId = Guid.NewGuid();
        var initialNodeLock = new NodeLock(NodeId.ToGraphNode(), state, false, Guid.NewGuid(), DateTime.UtcNow);

        var fixture = new GraphNodeStateRepositoryTestFixture()
            .WithNodeLock(initialNodeLock);

        (await fixture.Repository.UnOccupyNodeAsync(agvCorrelationId)).ShouldBeFalse();

        var nodeLock = fixture.Get(NodeId);
        nodeLock.ShouldNotBeNull();
        nodeLock.NodeId.ShouldBe(initialNodeLock.NodeId);
    }
}