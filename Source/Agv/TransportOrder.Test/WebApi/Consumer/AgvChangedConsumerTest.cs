using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class AgvChangedConsumerTest
{
    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Receiving_AgvOnlineChanged_SendAgvStateToAllClients(bool alive)
    {
        var agv = new TransportOrder.Domain.Models.Agv.Agv(AgvSerialNumber.ToGuid(), Guid.NewGuid(), AgvSerialNumber);
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

    [TestMethod]
    public async Task Consume_HasCargoChanged_ValidData_SentAgvHasCargoChanged()
    {
        var agvCorrelationId = Guid.NewGuid();
        const bool hasCargo = true;
        var fixture = new ConsumerHubTestFixture<AgvChangedConsumer, IAgvPublisher>();

        await fixture.TestHarness.Bus.Publish(new HasCargoChanged(agvCorrelationId, hasCargo));
        (await fixture.ConsumerHarness.Consumed.Any<HasCargoChanged>()).ShouldBeTrue();
        fixture.PublisherMock.Verify(h => h.SendAgvHasCargoChangedAsync(agvCorrelationId, hasCargo),
            Times.Once);
    }
}