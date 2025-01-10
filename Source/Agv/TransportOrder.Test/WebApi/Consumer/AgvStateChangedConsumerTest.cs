using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class AgvStateChangedConsumerTest
{
    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    [DataRow(AgvState.Busy, AgvState.Idle)]
    [DataRow(AgvState.Discovered, AgvState.Initial)]
    [DataRow(AgvState.Idle, AgvState.Discovered)]
    [DataRow(AgvState.Idle, AgvState.Busy)]
    public async Task Receiving_AgvStateChanged_WhenCurrentStateIsNotInitial_SendAgvStateChangedToAllClients(AgvState currentState, AgvState? previousState)
    {
        var agvStatus = new AgvStatus(Guid.NewGuid(), null, false, true, 100.0d, false, false, NodeGenerator.GenerateParkingNode().Id);
        var agv = new Agv(AgvSerialNumber.ToGuid(), Guid.NewGuid(), AgvSerialNumber, agvStatus);
        agv.SetOnline();
        agv = AgvGenerator.SetAgvState(currentState, agv, false);

        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<TransportOrder.Messages.MessageContent.Agv>(agv);

        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));

        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();
        await fixture.TestHarness.Bus.Publish(new AgvStateChanged(agvMessageContent.CorrelationId, agvMessageContent.CurrentState, previousState, agvMessageContent));
        
        await fixture.ShouldHavePublishedAsync<AgvStateChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvStateChanged>();

        fixture.PublisherMock.Verify(h => h.SendAgvStateChangedAsync(It.Is<TransportOrder.Messages.MessageContent.Agv>(x => x.CurrentState == currentState)), Times.Once);
    }

    [TestMethod]
    [DataRow(AgvState.Initial, null)]
    public async Task Receiving_AgvStateChanged_WhenCurrentStateIsInitial_SendAgvDiscoveredToAllClients(AgvState currentState, AgvState? previousState)
    {
        var agv = AgvGenerator.GenerateRawAgv(AgvSerialNumber.ToGuid(), AgvSerialNumber);
        agv = AgvGenerator.SetAgvState(currentState, agv, false);

        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<TransportOrder.Messages.MessageContent.Agv>(agv);

        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));

        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();
        await fixture.TestHarness.Bus.Publish(new AgvStateChanged(agvMessageContent.CorrelationId, agvMessageContent.CurrentState, previousState, agvMessageContent));
        
        await fixture.ShouldHavePublishedAsync<AgvStateChanged>();
        await fixture.ShouldHaveConsumedAsync<AgvStateChanged>();

        fixture.PublisherMock.Verify(h => h.SendAgvStateChangedAsync(
            It.Is<TransportOrder.Messages.MessageContent.Agv>(o => o.CorrelationId == agv.CorrelationId)), Times.Once);
    }
}