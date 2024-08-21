using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class AgvPositionChangedConsumerTest
{
    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    public async Task Receiving_AgvPositionChanged_SendsAgvPosition()
    {
        var fixture = new AgvPositionChangedConsumerTestFixture();
        await fixture.TestHarness.Bus.Publish(
            new AgvPositionChanged(
                AgvSerialNumber.ToGuid(),
                new AgvPosition(false, 0, 0, 0, 0),
                null,
                AgvSerialNumber
            ));
        
        await fixture.ShouldHavePublishedAsync<AgvPositionChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvPositionChanged>();
        fixture.PublisherMock.Verify(h => h.SendAgvPositionAsync(It.Is<AgvPositionStatus>(o => o.SerialNumber == AgvSerialNumber)), Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_OldLastNodeWasOccupiedByAgv_UnOccupyNode()
    {
        var oldLastNodeState = new NodeLock("old", NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(oldLastNodeState, null)
            .WithNodeOccupied(AgvSerialNumber.ToGuid());
        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));
        
        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepositoryMock.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Once);
    }

    /// <summary>
    /// Test case for chain of park nodes used as agv queue. Current agv passes parking one.
    /// </summary>
    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_OldLastNodeWasOccupiedByDifferentAgv_UnOccupyNodeNotCalled()
    {
        var oldLastNodeState = new NodeLock("old", NodeLockState.Occupied, false, "different".ToGuid(), DateTime.UtcNow);
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(oldLastNodeState, null)
            .WithNodeOccupied("different".ToGuid());
        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepositoryMock.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
        fixture.NodeStateRepositoryMock.Verify(r => r.UnOccupyNodeAsync("different".ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_OldLastNodeWasNotOccupied_UnOccupyNodeNotCalled()
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(null, null);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepositoryMock.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_NewLastNodeReservedForAgv_NewLastNodeOccupied()
    {
        var newLastNodeState = new NodeLock("new", NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(null, newLastNodeState)
            .WithNodeReserved("new");

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepositoryMock.Verify(r => r.OccupyNodeAsync("new", AgvSerialNumber.ToGuid()), Times.Once);
    }

    /// <summary>
    /// Test case for chain of park nodes used as agv queue. Current agv passes parking node reserved for different agv.
    /// </summary>
    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_NewLastNodeReservedForDifferentAgv_NewLastNodeNotOccupied()
    {
        var newLastNodeState = new NodeLock("new", NodeLockState.Reserved, false, "different".ToGuid(), DateTime.UtcNow);
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(null, newLastNodeState)
            .WithNodeReserved("new");

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepositoryMock.Verify(r => r.OccupyNodeAsync("new", "different".ToGuid()), Times.Never);
        fixture.NodeStateRepositoryMock.Verify(r => r.OccupyNodeAsync("new", AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_NewLastNodeNotReserved_NewLastNodeNotOccupied()
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(null, null);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepositoryMock.Verify(r => r.OccupyNodeAsync("new", AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_OldLastNodeWasOccupiedByAgv_NewLastNodeReservedForAgv_UnOccupyLastNode_OccupyNewNode()
    {
        var oldLastNodeState = new NodeLock("old", NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var newLastNodeState = new NodeLock("new", NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);

        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(oldLastNodeState, newLastNodeState)
            .WithNodeOccupied(AgvSerialNumber.ToGuid());
        
        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepositoryMock.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Once);
        fixture.NodeStateRepositoryMock.Verify(r => r.OccupyNodeAsync("new", AgvSerialNumber.ToGuid()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_AgvRouteIsNull_SendAgvRouteAsyncNotCalled()
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(null, null)
            .WithAgvRoute(AgvSerialNumber.ToGuid(), "new", null);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));
        
        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();
        fixture.PublisherMock.Verify(r => r.SendAgvRouteChangedAsync(It.IsAny<CurrentAgvRouteReadModel>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_AgvLastNodeIdChanged_AgvRouteIsNotNull_SendAgvRouteAsyncCalled()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, NodeGenerator.GenerateRouteNodes(), []);
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeStates(null, null)
            .WithAgvRoute(agvCorrelationId, "new", agvRoute);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                agvCorrelationId,
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<AgvLastNodeIdChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvLastNodeIdChanged>();

        fixture.AgvRouteServiceMock.Verify(r => r.GetAgvRouteAsync(agvCorrelationId, "new"), Times.Once);
        fixture.PublisherMock.Verify(r => r.SendAgvRouteChangedAsync(agvRoute), Times.Once);
    }
}