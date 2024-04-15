using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Consumer;

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

        (await fixture.TestHarness.Published.Any<AgvPositionChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvPositionChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendAgvPositionAsync(It.Is<AgvPositionStatus>(o => o.SerialNumber == AgvSerialNumber)), Times.Once);
    }

    [DataRow("oldLastNodeId", "newLastNodeId")]
    [DataRow("oldLastNodeId", null)]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_UnOccupyCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithOutNodeState()
            .WithNodeOccupied();
        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
        fixture.NodeStateRepositoryMock.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Once);
    }

    [DataRow(null, null)]
    [DataRow(null, "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_UnOccupyNotCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithOutNodeState();
        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
        fixture.NodeStateRepositoryMock.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_NodeReserved_OccupyNodeAsyncCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeTrue();
        fixture.NodeStateRepositoryMock.Verify(r => r.OccupyNodeAsync(newLastNodeId, AgvSerialNumber.ToGuid()), Times.Once);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_NodeNotReserved_OccupyNodeAsyncNotCalled(string? oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId, false);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<NodeStateChanged>()).ShouldBeFalse();
        fixture.NodeStateRepositoryMock.Verify(r => r.OccupyNodeAsync(newLastNodeId, AgvSerialNumber.ToGuid()), Times.Never);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [DataRow("oldLastNodeId", null)]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_GetAgvRouteAsyncCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId);

        var agvCorrelationId = AgvSerialNumber.ToGuid();
        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                agvCorrelationId,
                oldLastNodeId,
                newLastNodeId));

        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.AgvRouteServiceMock.Verify(r => r.GetAgvRouteAsync(agvCorrelationId, newLastNodeId), Times.Once);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [DataRow("oldLastNodeId", null)]
    [TestMethod]
    public async Task Receiving_WhenAgvRouteIsNull_SendAgvRouteAsyncNotCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId, false)
            .WithAgvRoute(AgvSerialNumber.ToGuid(), newLastNodeId, null);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvSerialNumber.ToGuid(),
                oldLastNodeId,
                newLastNodeId));

        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(r => r.SendAgvRouteChangedAsync(It.IsAny<CurrentAgvRouteReadModel>()), Times.Never);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [DataRow("oldLastNodeId", null)]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_AgvRouteIsNotNull_SendAgvRouteAsyncCalled(string oldLastNodeId, string newLastNodeId)
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new CurrentAgvRouteReadModel(agvCorrelationId, NodeGenerator.GenerateRouteNodes());
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId, false)
            .WithAgvRoute(agvCorrelationId, newLastNodeId, agvRoute);

        await fixture.TestHarness.Bus.Publish(
            new AgvLastNodeIdChanged(
                agvCorrelationId,
                oldLastNodeId,
                newLastNodeId));

        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.AgvRouteServiceMock.Verify(r => r.GetAgvRouteAsync(agvCorrelationId, newLastNodeId), Times.Once);
        fixture.PublisherMock.Verify(r => r.SendAgvRouteChangedAsync(agvRoute), Times.Once);
    }
}