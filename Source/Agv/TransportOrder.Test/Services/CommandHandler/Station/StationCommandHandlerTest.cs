using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Station;

[TestClass]
public class StationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_FreeNode_ValidData_FreeNodeCalled()
    {
        const string nodeId = "Charge-0";
        var fixture = new StationCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(nodeId, NodeLockState.Free, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(nodeId));
        
        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<string>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_LockStation_ValidData_NodeStateChangedCalled()
    {
        const string nodeId = "PD-0";

        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(nodeId)
            .WithLockableAndUnlockableNode(nodeId)
            .WithNodeState(new NodeLock(nodeId, NodeLockState.Free, false, null, DateTime.UtcNow));

        await fixture.TestHarness.Bus.Publish(new LockStation(nodeId.ToGuid(), "User", "Reason", "Description", LockedOrderHandlingType.AllowFinish));

        await fixture.ShouldHaveConsumedAsync<LockStation>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyStationUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UnlockStation_ValidData_NodeStateChangedCalled()
    {
        const string nodeId = "PD-0";

        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(nodeId)
            .WithLockableAndUnlockableNode(nodeId)
            .WithNodeState(new NodeLock(nodeId, NodeLockState.Free, true, null, DateTime.UtcNow));

        await fixture.TestHarness.Bus.Publish(new UnlockStation(nodeId.ToGuid()));

        await fixture.ShouldHaveConsumedAsync<UnlockStation>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyStationUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UnlockStation_NotUnlockableNode_NodeStateChangedNotCalled()
    {
        const string nodeId = "PD-0";

        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(nodeId);

        await fixture.TestHarness.Bus.Publish(new UnlockStation(nodeId.ToGuid()));

        await fixture.ShouldHaveConsumedAsync<UnlockStation>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }
}