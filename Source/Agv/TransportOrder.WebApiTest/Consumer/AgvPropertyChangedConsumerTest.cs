using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

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
        var agv = new Domain.Models.Agv.Agv(AgvId.ToGuid(), AgvId);
        agv.TransitionToDiscovered(true);
        agv.TransitionToIdle();
        agv.SetOnline(Guid.NewGuid());

        if (!alive)
        {
            agv.GoOffline((Guid)agv.AgvOfflineTokenId!);
        }

        var mapper = MappingHelper.GetMapper(new AgvProfile());
        var agvMessageContent = mapper.Map<Messages.MessageContent.Agv>(agv);

        var mockGetAgvsClient = new Mock<IRequestClient<GetAgvs>>();
        var responseMock = new Mock<Response<GotAgvs>>();
        responseMock.SetupGet(m => m.Message).Returns(new GotAgvs(new[] { agvMessageContent }));
        mockGetAgvsClient.Setup(
                m => m.GetResponse<GotAgvs>(It.IsAny<GetAgvs>(), default, default
                ))
            .ReturnsAsync(responseMock.Object);

        var fixture = new ConsumerHubTestFixture<AgvPropertyChangedConsumer, IAgvPublisher>(services => { services.AddSingleton(mockGetAgvsClient.Object); });
        await fixture.TestHarness!.Bus.Publish(
            new AgvPropertyChanged(
                agvMessageContent,
                agv.CorrelationId
            ));

        (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendAgvStateAsync(
            It.Is<Messages.MessageContent.Agv>(o => o.CorrelationId == agv.CorrelationId), false), Times.Once);
    }
}