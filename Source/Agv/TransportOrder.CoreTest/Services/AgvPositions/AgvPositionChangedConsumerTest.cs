using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.AgvPositions
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

            (await fixture.TestHarness.Published.Any<AgvPositionChanged>()).ShouldBeTrue();
            (await fixture.ConsumerHarness!.Consumed.Any<AgvPositionChanged>()).ShouldBeTrue();

            const string method = nameof(Defines.HubMethods.AgvPositionChanged);
            mockHub.Verify(h => h.SendAgvPositionToAllClients(method, It.Is<AgvPositionStatus>(o => o.AgvId == AgvId), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
