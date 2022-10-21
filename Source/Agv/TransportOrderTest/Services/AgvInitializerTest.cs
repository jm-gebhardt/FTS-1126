using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Tests for <see cref="AgvInitializer"/>.
    /// </summary>
    [TestClass]
    public class AgvInitializerTest
    {
        private const string AgvTestIdentifier01 = "#2410#";

        [TestMethod]
        public async Task When_MassTransitBusEventPostStarted_AgvInitializeMessageSent()
        {
            var mockAgvRegistry = new Mock<IAgvRegistry>();
            mockAgvRegistry.Setup(s => s.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>
            {
                new Messages.Agv() { CorrelationId = AgvTestIdentifier01.ToGuid(), AgvId = AgvTestIdentifier01 }
            }));
            var fixture = new MassTransitTestFixture<AgvInitializer>(
                services: s =>
                {
                    s.AddSingleton((_) => mockAgvRegistry.Object);
                },
                busRegistrationConfigurator: (b) =>
                {
                    b.AddConsumer<AgvInitializer>();
                }
            );

            await fixture.TestHarness!.Bus.Publish(new MassTransitBusEvent(MassTransitBusEvent.EventType.PostStarted));
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MassTransitBusEvent>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStateMachine.AgvInitializeMessage>());
        }
    }
}
