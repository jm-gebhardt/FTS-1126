using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Graphs
{
    [TestClass]
    public class EfNodeStateRegistryTest
    {
        [TestMethod]
        public async Task When_LockNodeAsync_RegistryEmpty_LockSuccessful()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            (await fixture.Registry.LockNodeAsync(nodeId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.IsLocked.ShouldBeTrue();
            nodeLock.AgvCorrelationId.ShouldBeNull();
        }
        
        [TestMethod]
        public async Task When_LockNodeAsync_NodeExists_LockSuccessful_AllOtherParameterUnchanged()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Free, false, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.LockNodeAsync(nodeId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
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
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Free, true, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.UnlockNodeAsync(nodeId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.IsLocked.ShouldBeFalse();
            nodeLock.NodeId.ShouldBe(initialNodeLock.NodeId);
            nodeLock.NodeState.ShouldBe(initialNodeLock.NodeState);
            nodeLock.AgvCorrelationId.ShouldBe(initialNodeLock.AgvCorrelationId);
            nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
        }
        
        [TestMethod]
        public async Task When_UnLockNodeAsync_NodeLocked_UnlockSuccessful()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Free, true, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.UnlockNodeAsync(nodeId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.IsLocked.ShouldBeFalse();
            nodeLock.NodeState.ShouldBe(initialNodeLock.NodeState);
            nodeLock.AgvCorrelationId.ShouldBe(initialNodeLock.AgvCorrelationId);
            nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
            
        }
        
        [TestMethod]
        public async Task When_UnLockNodeAsync_RegistryEmpty_UnLockFailed()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            (await fixture.Registry.UnlockNodeAsync(nodeId)).ShouldBeFalse();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldBeNull();
        }
        
        [TestMethod]
        public async Task When_UnLockNodeAsync_NodeAlreadyUnLocked_LockFailed()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Free, false, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.UnlockNodeAsync(nodeId)).ShouldBeFalse();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.ShouldBe(initialNodeLock);
        }
        
        [TestMethod]
        public async Task When_ReserveNodeAsync_RegistryEmpty_Successful()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            (await fixture.Registry.ReserveNodeAsync(nodeId, agvCorrelationId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.IsLocked.ShouldBeFalse();
            nodeLock.NodeState.ShouldBe(NodeLockState.Reserved);
            nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
        }
        
        [TestMethod]
        public async Task When_ReserveNodeAsync_NodeExists_Successful()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Free, false, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.ReserveNodeAsync(nodeId, agvCorrelationId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.IsLocked.ShouldBeFalse();
            nodeLock.NodeState.ShouldBe(NodeLockState.Reserved);
            nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
            nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
        }
        
        [TestMethod]
        public async Task When_ReserveNodeAsync_NodeOccupied_Failed()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.ReserveNodeAsync(nodeId, agvCorrelationId)).ShouldBeFalse();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.ShouldBe(initialNodeLock);
        }
        
        [TestMethod]
        public async Task When_ReserveNodeAsync_NodeLocked_Failed()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Free, true, null, DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.ReserveNodeAsync(nodeId, agvCorrelationId)).ShouldBeFalse();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.ShouldBe(initialNodeLock);
        }
        
        [TestMethod]
        public async Task When_UnReserveNodeAsync_NodeExists_Successful()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Reserved, false, agvCorrelationId, DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.UnReserveNodeAsync(agvCorrelationId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
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
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            
            var initialNodeLock = new NodeLock(nodeId, state, false, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.UnReserveNodeAsync(agvCorrelationId)).ShouldBeFalse();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.ShouldBe(initialNodeLock);
        }
        
        [TestMethod]
        public async Task When_OccupyNodeAsync_RegistryEmpty_Successful()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            (await fixture.Registry.OccupyNodeAsync(nodeId, agvCorrelationId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.IsLocked.ShouldBeFalse();
            nodeLock.NodeState.ShouldBe(NodeLockState.Occupied);
            nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
        }
        
        [TestMethod]
        public async Task When_OccupyNodeAsync_NodeExistsFree_Successful()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Free, false, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.OccupyNodeAsync(nodeId, agvCorrelationId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.IsLocked.ShouldBeFalse();
            nodeLock.NodeState.ShouldBe(NodeLockState.Occupied);
            nodeLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
            nodeLock.ModifiedDate.ShouldNotBe(initialNodeLock.ModifiedDate);
        }
        
        [TestMethod]
        public async Task When_OccupyNodeAsync_NodeExistsReserved_Successful()
        {
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            
            var initialNodeLock = new NodeLock(nodeId, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.OccupyNodeAsync(nodeId, agvCorrelationId)).ShouldBeTrue();

            var nodeLock = fixture.Get(nodeId);
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
            var fixture = new EfNodeStateRegistryTestFixture();
            const string nodeId = "TestNode";
            var agvCorrelationId = Guid.NewGuid();
            
            var initialNodeLock = new NodeLock(nodeId, state, false, Guid.NewGuid(), DateTime.Now);
            fixture.Add(initialNodeLock);
            
            (await fixture.Registry.UnOccupyNodeAsync(agvCorrelationId)).ShouldBeFalse();

            var nodeLock = fixture.Get(nodeId);
            nodeLock.ShouldNotBeNull();
            nodeLock.ShouldBe(initialNodeLock);
        }

    }
}


