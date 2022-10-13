using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Web;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// </summary>
    [TestClass]
    public class AgvPositionChangedConsumerTests
    {
        private const string AgvId = "agv1";

        /// <summary>
        /// </summary>
        [TestMethod]
        public async Task Receiving_AgvPositionChanged_SendsAgvPositionToAllClients()
        {
            var mockHub = new Mock<IAgvHubContext>();
            var fixture = new MassTransitTestFixture<AgvPositionChangedConsumer>(services =>
            {
                services.AddTransient(_ => mockHub.Object);
            });

            await fixture.TestHarness.Bus.Publish(
                new AgvPositionChanged(
                    new AgvPosition(false, 0, 0, 0, 0),
                    null,
                    AgvId,
                    AgvId.ToGuid()
                ));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvPositionChanged>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AgvPositionChanged>());

            const string method = nameof(Defines.HubMethods.AgvPositionChanged);
            mockHub.Verify(h => h.SendAgvPositionToAllClients(method, It.Is<AgvPositionReceived>(o => o.AgvId == AgvId), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
