using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services.AgvPositions
{
    [TestClass]
    public class AgvPositionChangedConsumerTest
    {
        private const string AgvId = "agv1";

        [TestMethod]
        public async Task Receiving_AgvPositionChanged_SendsAgvPositionToAllClients()
        {
            var mockHub = new Mock<IAgvHubContext>();
            var fixture = new MassTransitTestFixture<AgvPositionChangedConsumer>(services =>
            {
                services.AddTransient(_ => mockHub.Object);
                services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            });

            await fixture.TestHarness!.Bus.Publish(
                new AgvPositionChanged(
                    new AgvPosition(false, 0, 0, 0, 0),
                    null,
                    AgvId,
                    AgvId.ToGuid()
                ));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvPositionChanged>());
            Assert.IsTrue(await fixture.ConsumerHarness!.Consumed.Any<AgvPositionChanged>());

            const string method = nameof(Defines.HubMethods.AgvPositionChanged);
            mockHub.Verify(h => h.SendAgvPositionToAllClients(method, It.Is<AgvPositionStatus>(o => o.AgvId == AgvId), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
