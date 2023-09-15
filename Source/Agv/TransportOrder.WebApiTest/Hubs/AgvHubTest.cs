using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs;

[TestClass]
public class AgvHubTest
{
    private const string AgvId = "AGV001";

    [TestMethod]
    public async Task SendAgvStateAsync_SendsAgvDto_To_Discovered()
    {
        HubFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new HubFixture<AgvPublisher, AgvHub, IAgvClient>()
            .WithMapper(new AgvProfile(), new global::TransportOrder.Messages.Profiles.AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvPositionProfile())
            .Build();
        var agv = fixture.Mapper.Map<global::TransportOrder.Messages.MessageContent.Agv>(AgvGenerator.GenerateIdleAgv());

        await fixture.Publisher.SendAgvStateAsync(agv, true);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvDiscovered(It.Is<AgvDto>(a => a.CorrelationId == agv.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvStateAsync_SendsAgvDto_To_AgvChanged()
    {
        HubFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new HubFixture<AgvPublisher, AgvHub, IAgvClient>()
            .WithMapper(new AgvProfile(), new global::TransportOrder.Messages.Profiles.AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvPositionProfile())
            .Build();
        var agv = fixture.Mapper.Map<global::TransportOrder.Messages.MessageContent.Agv>(AgvGenerator.GenerateIdleAgv());

        await fixture.Publisher.SendAgvStateAsync(agv, false);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvChanged(It.Is<AgvDto>(a => a.CorrelationId == agv.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvPositionAsync_SendsAgvPositionStatusDto_To_AgvPositionChanged()
    {
        HubFixture<AgvPublisher, AgvHub, IAgvClient> fixture = new HubFixture<AgvPublisher, AgvHub, IAgvClient>()
            .WithMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile(), new AgvPositionProfile())
            .Build();
        var agvPositionStatus = new AgvPositionStatus(AgvId.ToGuid(), AgvId, new AgvPosition(false, 0, 0, 0, 0));

        await fixture.Publisher.SendAgvPositionAsync(agvPositionStatus);

        fixture.HubContextMock.Verify(c => c.Clients.All.AgvPositionChanged(It.Is<AgvPositionStatusDto>(a => a.CorrelationId == agvPositionStatus.CorrelationId)), Times.Once);
    }
}