using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs
{
    [TestClass]
    public class ActionHubTest
    {
        private Mock<IHubContext<ActionHub, IActionHubClient>> GetMock()
        {
            var mockHubClient = new Mock<IActionHubClient>();
            var mockClients = new Mock<IHubClients<IActionHubClient>>();
            var mockHubContext = new Mock<IHubContext<ActionHub, IActionHubClient>>();

            mockClients.SetupGet(h => h.All).Returns(mockHubClient.Object);
            mockHubContext.SetupGet(h => h.Clients).Returns(mockClients.Object);

            return mockHubContext;
        }

        [TestMethod]
        public async Task SendActionGroupStateToAllClients_ActionGroupChanged()
        {
            Mock<IHubContext<ActionHub, IActionHubClient>> mockHubContext = GetMock();
            var mapper = MappingHelper.GetMapper(new List<Profile> { new ActionProfile() });
            var actionHubContext = new ActionHubContext(mockHubContext.Object, mapper);

            const string orderId = "Order123";
            var correlationId = orderId.ToGuid();

            var actionGroup = new ActionGroup(correlationId, orderId);
            await actionHubContext.SendActionGroupStateToAllClients(actionGroup);

            mockHubContext.Verify(c => c.Clients.All.ActionGroupChanged(It.Is<ActionGroupDto>(a => a.CorrelationId == actionGroup.CorrelationId)), Times.Once);
        }
    }
}
