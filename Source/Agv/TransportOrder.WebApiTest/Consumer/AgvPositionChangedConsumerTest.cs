using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class AgvPositionChangedConsumerTest
{
    private const string AgvId = "agv1";

    [TestMethod]
    public async Task Receiving_AgvPositionChanged_SendsAgvPosition()
    {
        var fixture = new AgvPositionChangedConsumerTestFixture();
        await fixture.TestHarness!.Bus.Publish(
            new AgvPositionChanged(
                new AgvPosition(false, 0, 0, 0, 0),
                null,
                AgvId,
                AgvId.ToGuid()
            ));

        (await fixture.TestHarness.Published.Any<AgvPositionChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvPositionChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendAgvPositionAsync(It.Is<AgvPositionStatus>(o => o.AgvId == AgvId)), Times.Once);
    }

    [DataRow("oldLastNodeId", "newLastNodeId")]
    [DataRow("oldLastNodeId", null)]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_UnOccupyCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture();
        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.NodeStateRegistryMock.Verify(r => r.UnOccupyNodeAsync(AgvId.ToGuid()), Times.Once);
    }

    [DataRow(null, null)]
    [DataRow(null, "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_UnOccupyNotCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture();
        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.NodeStateRegistryMock.Verify(r => r.UnOccupyNodeAsync(AgvId.ToGuid()), Times.Never);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_NodeReserved_OccupyNodeAsyncCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId);

        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.NodeStateRegistryMock.Verify(r => r.OccupyNodeAsync(newLastNodeId, AgvId.ToGuid()), Times.Once);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_NodeNotReserved_OccupyNodeAsyncNotCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId, false);

        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.NodeStateRegistryMock.Verify(r => r.OccupyNodeAsync(newLastNodeId, AgvId.ToGuid()), Times.Never);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [DataRow("oldLastNodeId", null)]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_GetAgvRouteAsyncCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId);

        var agvCorrelationId = AgvId.ToGuid();
        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                agvCorrelationId,
                oldLastNodeId,
                newLastNodeId));

        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

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
            .WithAgvRoute(AgvId.ToGuid(), newLastNodeId, null);

        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));

        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(r => r.SendAgvRouteAsync(It.IsAny<AgvRouteReadModel>()), Times.Never);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [DataRow("oldLastNodeId", null)]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_AgvRouteIsNotNull_SendAgvRouteAsyncCalled(string oldLastNodeId, string newLastNodeId)
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvRoute = new AgvRouteReadModel(agvCorrelationId, NodeGenerator.GenerateRouteNodes());
        var fixture = new AgvPositionChangedConsumerTestFixture()
            .WithNodeReserved(newLastNodeId, false)
            .WithAgvRoute(agvCorrelationId, newLastNodeId, agvRoute);

        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                agvCorrelationId,
                oldLastNodeId,
                newLastNodeId));

        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.AgvRouteServiceMock.Verify(r => r.GetAgvRouteAsync(agvCorrelationId, newLastNodeId), Times.Once);
        fixture.PublisherMock.Verify(r => r.SendAgvRouteAsync(agvRoute), Times.Once);
    }
}