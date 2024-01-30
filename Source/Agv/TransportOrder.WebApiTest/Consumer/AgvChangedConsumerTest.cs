using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class AgvChangedConsumerTest
{
    private const string AgvId = "agv1";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Receiving_AgvOnlineChanged_SendAgvStateToAllClients(bool alive)
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

        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>(services => { services.AddSingleton(mockGetAgvsClient.Object); });
        await fixture.TestHarness.Bus.Publish(
            new AgvOnlineChanged(
                agv.CorrelationId,
                true
            ));

        (await fixture.ConsumerHarness.Consumed.Any<AgvOnlineChanged>()).ShouldBeTrue();

        fixture.PublisherMock.Verify(h => h.SendAgvOnlineChangedAsync(
            agv.CorrelationId, true), Times.Once);
    }

    [TestMethod]
    public async Task Consume_AgvBecameIdle_ValidData_SentAgvRouteChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();

        await fixture.TestHarness.Bus.Publish(new AgvBecameIdle(agvCorrelationId));
        (await fixture.ConsumerHarness.Consumed.Any<AgvBecameIdle>()).ShouldBeTrue();
        fixture.PublisherMock.Verify(h => h.SendAgvRouteChangedAsync(new CurrentAgvRouteReadModel(agvCorrelationId, Array.Empty<string>())),
            Times.Once);
    }
}