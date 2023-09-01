using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Profiles;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class AgvPropertyChangedConsumerTest
{
    private const string AgvId = "agv1";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Receiving_AgvPropertyChanged_SendAgvStateToAllClients(bool alive)
    {
        var agv = new AgvSaga(AgvId.ToGuid(), AgvId, nameof(AgvStateMachine.Idle)) { IsOnline = alive };
            
        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<global::TransportOrder.Messages.MessageContent.Agv>(agv);
            
        var mockGetAgvsClient = new Mock<IRequestClient<GetAgvs>>();
        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));
        mockGetAgvsClient.Setup(
                m => m.GetResponse<GotAgvs>(It.IsAny<GetAgvs>(), default, default
                ))
            .ReturnsAsync(responseMock.Object);
            
        var fixture = new ConsumerHubTestFixture<AgvPropertyChangedConsumer, IAgvHubContext>(services =>
        {
            services.AddSingleton(mockGetAgvsClient.Object);
        });
        await fixture.TestHarness!.Bus.Publish(
            new AgvPropertyChanged(
                agv,
                agv.CorrelationId
            ));

        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();

        fixture.MockHub.Verify(h => h.SendAgvStateToAllClients(
            It.Is<AgvDto>(o => o.CorrelationId == agv.CorrelationId), false), Times.Once);
    }
}