using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs;

[TestClass]
public class AgvPublisherTest
{
    private const string AgvId = "AGV001";

    [TestMethod]
    public async Task SendAgvStateAsync_SendsAgvDto_To_AgvDiscovered()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>()
            .WithMapper(new AgvProfile(), new Messages.Profiles.AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvPositionProfile())
            .Build();
        var agv = fixture.Mapper.Map<Messages.MessageContent.Agv>(AgvGenerator.GenerateIdleAgv());

        await fixture.Publisher.SendAgvDiscoveredAsync(agv);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvDiscovered(It.Is<AgvDto>(a => a.CorrelationId == agv.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvPositionAsync_SendsAgvPositionStatusDto_To_AgvPositionChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>()
            .WithMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvPositionProfile())
            .Build();
        var agvPositionStatus = new AgvPositionStatus(AgvId.ToGuid(), AgvId, new AgvPosition(false, 0, 0, 0, 0));

        await fixture.Publisher.SendAgvPositionAsync(agvPositionStatus);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvPositionChanged(It.Is<AgvPositionStatusDto>(a => a.CorrelationId == agvPositionStatus.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvRouteAsync_SendsAgvRouteDto_To_AgvRouteChanged()
    {
        PublisherFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new PublisherFixture<AgvPublisher, AgvHub, IAgvClient>()
            .WithMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvPositionProfile())
            .Build();
        var agvRoute = new AgvRouteReadModel(Array.Empty<string>());

        await fixture.Publisher.SendAgvRouteAsync(agvRoute);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvRouteChanged(It.IsAny<AgvRouteDto>()), Times.Once);
        fixture.HubContextMock.Setup(c => c.Clients.All.AgvRouteChanged(It.IsAny<AgvRouteDto>()))
            .Callback<AgvRouteDto>(a => a.RemainingRoute.ShouldBe(agvRoute.RemainingRoute));
    }
}