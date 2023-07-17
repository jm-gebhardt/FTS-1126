using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs
{
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
        public async Task SendAgvStateToAllClients_AgvDiscovered()
        {
            Mock<IHubContext<AgvHub, IAgvHubClient>> mockHubContext = GetMock();
            var mapper = MappingHelper.GetMapper(new AgvProfile());
            var agvHubContext = new AgvHubContext(mockHubContext.Object, mapper);

            var agv = new AgvSaga(AgvId.ToGuid(), AgvId);
            await agvHubContext.SendAgvStateToAllClients(agv, true);

            mockHubContext.Verify(c => c.Clients.All.AgvDiscovered(It.Is<AgvDto>(a => a.CorrelationId == agv.CorrelationId)), Times.Once);
        }

        [TestMethod]
        public async Task SendAgvStateToAllClients_AgvChanged()
        {
            Mock<IHubContext<AgvHub, IAgvHubClient>> mockHubContext = GetMock();
            var mapper = MappingHelper.GetMapper(new AgvProfile());
            var agvHubContext = new AgvHubContext(mockHubContext.Object, mapper);

            var agv = new AgvSaga(AgvId.ToGuid(), AgvId);
            await agvHubContext.SendAgvStateToAllClients(agv, false);

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
}