using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services
{
    /// <summary>
    /// Tests for <see cref="BusObserver"/>.
    /// </summary>
    [TestClass]
    public class BusObserverTest
    {
        [TestMethod]
        public async Task When_BusStarted_MassTransitBusEventPostStartedIsSent()
        {
            var fixture = new MassTransitTestFixture(
                services: _ => { },
                busRegistrationConfigurator: (_) => { },
                inMemoryConfiguration: (_, cfg) => { cfg.ConnectBusObserver(new BusObserver()); }
            );

            (await fixture.TestHarness.Published.Any<MassTransitBusEvent>()).ShouldBeTrue();
            fixture.TestHarness.Published.Select<MassTransitBusEvent>(s => s.Context.Message.Type == MassTransitBusEvent.EventType.PostStarted).First().ShouldNotBeNull();
        }
    }
}
