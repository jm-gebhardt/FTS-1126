using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// AGV position registry test
    /// </summary>
    [TestClass]
    public class AgvPositionRegistryTest
    {
        private const string AgvTestIdentifier01 = "#2410#";
        private const string AgvTestIdentifier02 = "#0815#";


        /// <summary>
        /// Test creation of registry and try to access AGV that doesn't exits.
        /// </summary>
        [TestMethod]
        public async Task PublishPosition_RegistryCreated_AccessWrongAgv_NotFound()
        {
            var fixture = new MassTransitTestFixture<AgvPositionReceivedConsumer>(services =>
            {
                services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
            });

            await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(AgvTestIdentifier01.ToGuid(), AgvTestIdentifier01, false, 0, 0, 0));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvPositionReceived>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>());

            var registry = fixture.Provider.GetRequiredService<IAgvPositionRegistry>();
            Assert.IsNotNull(registry);

            var position = registry.GetPosition(Guid.Empty);
            Assert.IsNull(position);
        }

        /// <summary>
        /// Test access AGV position received by the registry
        /// </summary>
        [TestMethod]
        public async Task PublishTwoPosition_RegistryCreated_AccessOneAgv_Found()
        {
            const bool init1 = true;
            const float x1 = 1f;
            const float y1 = 2f;
            const float theta1 = 3f;
            const float deviation1 = 0.123f;
            const bool init2 = false;
            const float x2 = 0f;
            const float y2 = 0f;
            const float theta2 = 0f;

            var fixture = new MassTransitTestFixture<AgvPositionReceivedConsumer>(services =>
            {
                services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
            });

            await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(AgvTestIdentifier01.ToGuid(), AgvTestIdentifier01, init1, x1, y1, theta1, deviation1));
            await fixture.TestHarness.Bus.Publish(new AgvPositionReceived(AgvTestIdentifier02.ToGuid(), AgvTestIdentifier02, init2, x2, y2, theta2));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvPositionReceived>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>());

            var registry = fixture.Provider.GetRequiredService<IAgvPositionRegistry>();
            Assert.IsNotNull(registry);

            var position = registry.GetPosition(AgvTestIdentifier01.ToGuid());
            Assert.IsNotNull(position);
            Assert.AreEqual(init1, position.PositionInitialized);
            Assert.AreEqual(y1, position.Y, 0.00001);
            Assert.AreEqual(deviation1, position.DeviationRange!.Value, 0.00001);

            position = registry.GetPosition(AgvTestIdentifier02.ToGuid());
            Assert.IsNotNull(position);
            Assert.AreEqual(x2, position.X, 0.00001);
            Assert.AreEqual(y2, position.Y, 0.00001);
            Assert.IsNull(position.DeviationRange);
        }
    }
}
