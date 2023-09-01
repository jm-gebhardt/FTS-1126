using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Messages.Requests.Agv;
using AgvPositionStatus = TransportOrder.Messages.Requests.Agv.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs;

[TestClass]
public class AgvHubTest
{
    private const string AgvId = "AGV001";

    private Mock<IHubContext<AgvHub, IAgvHubClient>> GetMock()
    {
        var mockHubClient = new Mock<IAgvHubClient>();
        var mockClients = new Mock<IHubClients<IAgvHubClient>>();
        var mockHubContext = new Mock<IHubContext<AgvHub, IAgvHubClient>>();

        mockClients.SetupGet(h => h.All).Returns(mockHubClient.Object);
        mockHubContext.SetupGet(h => h.Clients).Returns(mockClients.Object);

        return mockHubContext;
    }

    [TestMethod]
    public async Task SendAgvStateToAllClients_ValidData_DiscoveredAgvDtoSent()
    {
        Mock<IHubContext<AgvHub, IAgvHubClient>> mockHubContext = GetMock();
        var mapper = MappingHelper.GetMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile());
        var agvHubContext = new AgvHubContext(mockHubContext.Object, mapper);

        var agv = new global::TransportOrder.Messages.MessageContent.Agv(Guid.NewGuid(), "AGV", AgvState.Initial, DateTime.UtcNow, DateTime.UtcNow, false, true, false, true, 100.0d, false, 100, 100,
            null, new List<AgvError>() );
        var agvDto = mapper.Map<AgvDto>(agv);
        await agvHubContext.SendAgvStateToAllClients(agvDto, true);

        mockHubContext.Verify(c => c.Clients.All.AgvDiscovered(It.Is<AgvDto>(a => a.CorrelationId == agv.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvStateToAllClients_ValidData_ChangedAgvDtoSent()
    {
        Mock<IHubContext<AgvHub, IAgvHubClient>> mockHubContext = GetMock();
        var mapper = MappingHelper.GetMapper(new AgvProfile(), new ActionProfile(), new AgvErrorProfile());
        var agvHubContext = new AgvHubContext(mockHubContext.Object, mapper);

        var agv = new global::TransportOrder.Messages.MessageContent.Agv(Guid.NewGuid(), "AGV", AgvState.Initial, DateTime.UtcNow, DateTime.UtcNow, false, true, false, true, 100.0d, false, 100, 100,
            null, new List<AgvError>());
        var agvDto = mapper.Map<AgvDto>(agv);
        await agvHubContext.SendAgvStateToAllClients(agvDto, false);

        mockHubContext.Verify(c => c.Clients.All.AgvChanged(It.Is<AgvDto>(a => a.CorrelationId == agv.CorrelationId)), Times.Once);
    }

    [TestMethod]
    public async Task SendAgvPositionToAllClients_AgvPositionChanged()
    {
        Mock<IHubContext<AgvHub, IAgvHubClient>> mockHubContext = GetMock();
        var mapper = MappingHelper.GetMapper(new AgvPositionProfile());

        var agvHubContext = new AgvHubContext(mockHubContext.Object, mapper);

        var agv = new AgvSaga(AgvId.ToGuid(), AgvId);
        var agvPosition = new Common.Messages.AgvManagement.AgvPosition(true, 1.0f, 1.0f, 1f, 0f, 1.0f);
        var agvPositionStatus = new AgvPositionStatus(agv.CorrelationId, agv.AgvId, agvPosition);

        await agvHubContext.SendAgvPositionToAllClients(agvPositionStatus);

        mockHubContext.Verify(c => c.Clients.All.AgvPositionChanged(It.Is<AgvPositionStatusDto>(a => a.CorrelationId == agv.CorrelationId)), Times.Once);
    }
}