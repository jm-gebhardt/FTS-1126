using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Station;

[TestClass]
public class StationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_LockStation_ValidData_NodeStateUpdated()
    {
        var node = "PD-0".ToGraphNode();
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        const string user = "User";
        const string reason = "Reason";
        const string description = "Description";

        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(node.Name)
            .WithNode(node);

        await fixture.TestHarness.Bus.Publish(new LockStation(node.Id, user, reason, description, LockedOrderHandlingType.AllowFinish));

        await fixture.ShouldHaveConsumedAsync<LockStation>();
        fixture.VerifyStationUpdated(Times.Once());
        fixture.NodeRepository.Verify(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        var nodeStateChanged = fixture.TestHarness.Published.Select<NodeStateChanged>().First().Context.Message;
        nodeStateChanged.LockDescription.ShouldBe(description);
        nodeStateChanged.LockReason.ShouldBe(reason);
        nodeStateChanged.ModifyingUser.ShouldBe(user);
        nodeStateChanged.StationId.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_UnlockStation_ValidData_NodeStateUpdated()
    {
        var node = "PD-0".ToGraphNode();
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);
        const string user = "User";
        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(node.Name)
            .WithNode(node);

        await fixture.TestHarness.Bus.Publish(new UnlockStation(node.Id, user));

        await fixture.ShouldHaveConsumedAsync<UnlockStation>();
        fixture.VerifyStationUpdated(Times.Once());
        fixture.NodeRepository.Verify(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        var nodeStateChanged = fixture.TestHarness.Published.Select<NodeStateChanged>().First().Context.Message;
        nodeStateChanged.LockDescription.ShouldBeNull();
        nodeStateChanged.LockReason.ShouldBeNull();
        nodeStateChanged.ModifyingUser.ShouldBe(user);
        nodeStateChanged.StationId.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_UnlockStation_NotUnlockableNode_NodeStateChangedNotCalled()
    {
        const string nodeName = "PD-0";
        var node = nodeName.ToGraphNode();
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(nodeName)
            .WithNode(node);

        var nodeId = nodeName.ToGuid();
        await fixture.TestHarness.Bus.Publish(new UnlockStation(nodeId, "User"));

        await fixture.ShouldHaveConsumedAsync<UnlockStation>();
        fixture.VerifyStationUpdated(Times.Never());
        fixture.NodeRepository.Verify(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_MapSaved_NoRelatedAgvs_DoesNotAddStations()
    {
        var fixture = new StationCommandHandlerTestFixture()
            .WithAgvsRelatedToNodes([]);

        await fixture.TestHarness.Bus.Publish(new Common.Messages.Maps.MapSaved(new Common.Domain.Models.Graphs.Map(Guid.NewGuid(), "mapId", DateTime.UtcNow, "image", 1, 1, 23, 0, 0)));

        await fixture.ShouldHaveConsumedAsync<Common.Messages.Maps.MapSaved>();
        fixture.VerifyStationsAdded(Times.Never());
    }

    [TestMethod]
    public async Task Consume_MapSaved_ValidData_AddsStations()
    {
        var fixture = new StationCommandHandlerTestFixture()
            .WithAgvsRelatedToNodes([Guid.NewGuid()])
            .WithAgvsGraph();

        await fixture.TestHarness.Bus.Publish(new Common.Messages.Maps.MapSaved(new Common.Domain.Models.Graphs.Map(Guid.NewGuid(), "mapId", DateTime.UtcNow, "image", 1, 1, 23, 0, 0)));

        await fixture.ShouldHaveConsumedAsync<Common.Messages.Maps.MapSaved>();
        fixture.VerifyStationsAdded(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateStationJamState_ValidData_PublishesJamStateChanged()
    {
        const string station = "station";
        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UpdateStationJamState(station.ToGuid(), true));

        await fixture.ShouldHavePublishedAsync<JamStateChanged>();
        fixture.VerifyStationUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateStationJamState_UnchangedJamState_PublishJamStateChanged()
    {
        const string station = "station";
        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UpdateStationJamState(station.ToGuid(), false));

        await fixture.ShouldHavePublishedAsync<JamStateChanged>();
        fixture.VerifyStationUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateStationAdditionalInformation_StationUpdated()
    {
        const string station = "station";
        var fixture = new StationCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UpdateStationAdditionalInfo(station.ToGuid(), new Dictionary<string, object?>{{"NeedsCargoReporting", true}}));

        fixture.VerifyStationUpdated(Times.Once());
    }
}