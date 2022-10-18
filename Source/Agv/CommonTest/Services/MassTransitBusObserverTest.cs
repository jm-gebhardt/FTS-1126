using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services
{
    /// <summary>
    /// Tests for <see cref="BusObserver"/>.
    /// </summary>
    [TestClass]
    public class BusObserverTest
    {
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task When_BusStarted_MassTransitBusEventPostStartedIsSent()
        {
            var fixture = new MassTransitTestFixture(
                services: _ => { },
                busRegistrationConfigurator: (_) => { },
                inMemoryConfiguration: (_, cfg) => { cfg.ConnectBusObserver(new BusObserver()); }
            );

            Assert.IsTrue(await fixture.TestHarness.Published.Any<MassTransitBusEvent>());
            Assert.IsNotNull(fixture.TestHarness.Published.Select<MassTransitBusEvent>(s => s.Context.Message.Type == MassTransitBusEvent.EventType.PostStarted).First());
        }
    }
}
