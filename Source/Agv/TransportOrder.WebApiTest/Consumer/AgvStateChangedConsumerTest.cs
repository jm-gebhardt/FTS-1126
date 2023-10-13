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
    [DataRow(AgvState.Initial, null)]
    [DataRow(AgvState.Discovered, AgvState.Initial)]
    [DataRow(AgvState.Idle, AgvState.Discovered)]
    [DataRow(AgvState.Busy, AgvState.Idle)]
    [DataRow(AgvState.Idle, AgvState.Busy)]
    public async Task Receiving_AgvStateChanged_SendAgvStateToAllClients(AgvState currentState, AgvState? previousState)
    {
        var agv = new TransportOrder.Domain.Models.Agv.Agv(AgvId.ToGuid(), AgvId);
        agv = AgvGenerator.SetAgvState(currentState, false, agv);

        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.Agv>(agv);
            
        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));

        var fixture = new ConsumerHubTestFixture<AgvStateChangedConsumer, IAgvPublisher>();
        await fixture.TestHarness!.Bus.Publish(new AgvStateChanged(agvMessageContent.CorrelationId, agvMessageContent, previousState));

        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvStateChanged>()).ShouldBeTrue();
            
            
        bool isInitialState = currentState == AgvState.Initial;
        fixture.PublisherMock.Verify(h => h.SendAgvStateAsync(
            It.Is<Messages.MessageContent.Agv>(o => o.CorrelationId == agv.CorrelationId), isInitialState), Times.Once);
    }
}