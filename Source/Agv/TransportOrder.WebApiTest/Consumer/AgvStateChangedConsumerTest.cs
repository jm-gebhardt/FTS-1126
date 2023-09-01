using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Profiles;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class AgvStateChangedConsumerTest
{
    private const string AgvId = "agv1";

    [TestMethod]
    [DataRow(nameof(AgvStateMachine.Initial), null)]
    [DataRow(nameof(AgvStateMachine.Discovered), nameof(AgvStateMachine.Initial))]
    [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Discovered))]
    [DataRow(nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Idle))]
    [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Busy))]
    public async Task Receiving_AgvStateChanged_SendAgvStateToAllClients(string currentState, string? previousState)
    {
        var agv = new AgvSaga(AgvId.ToGuid(), AgvId, currentState);
            
        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<global::TransportOrder.Messages.MessageContent.Agv>(agv);
            
        var mockGetAgvsClient = new Mock<IRequestClient<GetAgvs>>();
        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));
        mockGetAgvsClient.Setup(
                m => m.GetResponse<GotAgvs>(It.IsAny<GetAgvs>(), default, default
                ))
            .ReturnsAsync(responseMock.Object);

        var fixture = new ConsumerHubTestFixture<AgvStateChangedConsumer, IAgvHubContext>(
            services =>
            {
                services.AddSingleton(mockGetAgvsClient.Object);
            });
        await fixture.TestHarness!.Bus.Publish(new AgvStateChanged(agv.CorrelationId, agv, previousState));

        (await fixture.TestHarness.Published.Any<AgvStateChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvStateChanged>()).ShouldBeTrue();
            
        mockGetAgvsClient.Verify(m => m.GetResponse<GotAgvs>(It.IsAny<GetAgvs>(), default, default), Times.Once);
            
        bool isInitialState = currentState == nameof(AgvStateMachine.Initial);
        fixture.MockHub.Verify(h => h.SendAgvStateToAllClients(
            It.Is<AgvDto>(o => o.CorrelationId == agv.CorrelationId), isInitialState), Times.Once);
    }
}