using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class AgvStateChangedConsumerTest
{
    private const string AgvId = "agv1";

    [TestMethod]
    [DataRow(AgvState.Busy, AgvState.Idle)]
    [DataRow(AgvState.Discovered, AgvState.Initial)]
    [DataRow(AgvState.Idle, AgvState.Discovered)]
    [DataRow(AgvState.Idle, AgvState.Busy)]
    public async Task Receiving_AgvStateChanged_WhenCurrentStateIsNotInitial_SendAgvStateChangedToAllClients(AgvState currentState, AgvState? previousState)
    {
        var agvStatus = new AgvStatus(Guid.NewGuid(), null, false, true, 100.0d, false, false, NodeGenerator.GenerateParkingNode().Id);
        var agv = new Domain.Models.Agv.Agv(AgvId.ToGuid(), AgvId, agvStatus);
        agv = AgvGenerator.SetAgvState(currentState, false, agv);

        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<Messages.MessageContent.Agv>(agv);

        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));

        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();
        await fixture.TestHarness.Bus.Publish(new AgvStateChanged(agvMessageContent.CorrelationId, agvMessageContent.CurrentState, previousState, agvMessageContent));

        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvStateChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendAgvStateChangedAsync(It.Is<Messages.MessageContent.Agv>(x => x.CurrentState == currentState)), Times.Once);
    }

    [TestMethod]
    [DataRow(AgvState.Initial, null)]
    public async Task Receiving_AgvStateChanged_WhenCurrentStateIsInitial_SendAgvDiscoveredToAllClients(AgvState currentState, AgvState? previousState)
    {
        var agv = new Domain.Models.Agv.Agv(AgvId.ToGuid(), AgvId);
        agv = AgvGenerator.SetAgvState(currentState, false, agv);

        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<Messages.MessageContent.Agv>(agv);

        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));

        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();
        await fixture.TestHarness.Bus.Publish(new AgvStateChanged(agvMessageContent.CorrelationId, agvMessageContent.CurrentState, previousState, agvMessageContent));

        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<AgvStateChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendAgvStateChangedAsync(
            It.Is<Messages.MessageContent.Agv>(o => o.CorrelationId == agv.CorrelationId)), Times.Once);
    }
}