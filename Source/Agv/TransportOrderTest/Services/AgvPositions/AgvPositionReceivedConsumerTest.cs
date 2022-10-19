using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services.AgvPositions
{
    /// <summary>
    /// Test for AgvPositionReceivedConsumer
    /// </summary>
    [TestClass]
    public class AgvPositionReceivedConsumerTest
    {
        private const string AgvTestIdentifier01 = "#2410#";
        private const string AgvTestIdentifier02 = "#0815#";

        /// <summary>
        /// Test creation of registry and try to access AGV that doesn't exits.
        /// </summary>
        [TestMethod]
        public async Task PublishPosition_RegistryCreated_AccessWrongAgv_NotFound()
        {
            var fixture = new AgvPositionReceivedConsumerMassTransitTestFixture();

            await fixture.TestHarness.Bus.Publish(
                new AgvPositionReceived(
                    AgvTestIdentifier01.ToGuid(),
                    AgvTestIdentifier01,
                    new AgvPosition(false, 0, 0, 0, 0)
                ));

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
            const float z1 = 3f;
            const float theta1 = 3f;
            const float deviation1 = 0.123f;
            const bool init2 = false;
            const float x2 = 0f;
            const float y2 = 0f;
            const float z2 = 0f;
            const float theta2 = 0f;

            var fixture = new AgvPositionReceivedConsumerMassTransitTestFixture();

            await fixture.TestHarness.Bus.Publish(
                new AgvPositionReceived(
                    AgvTestIdentifier01.ToGuid(),
                    AgvTestIdentifier01,
                    new AgvPosition(init1, x1, y1, z1, theta1, deviation1)
                ));
            await fixture.TestHarness.Bus.Publish(
                new AgvPositionReceived(
                    AgvTestIdentifier02.ToGuid(),
                    AgvTestIdentifier02,
                    new AgvPosition(init2, x2, y2, z2, theta2)
                ));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvPositionReceived>());
            Assert.AreEqual(2, fixture.ConsumerHarness.Consumed.Select<AgvPositionReceived>().Count());

            var registry = fixture.Provider.GetRequiredService<IAgvPositionRegistry>();
            Assert.IsNotNull(registry);

            var position = registry.GetPosition(AgvTestIdentifier01.ToGuid());
            Assert.IsNotNull(position);
            Assert.AreEqual(init1, position.AgvPosition.PositionInitialized);
            Assert.AreEqual(x1, position.AgvPosition.X, 0.00001);
            Assert.AreEqual(y1, position.AgvPosition.Y, 0.00001);
            Assert.AreEqual(z1, position.AgvPosition.Z, 0.00001);
            Assert.AreEqual(theta1, position.AgvPosition.Theta, 0.00001);
            Assert.AreEqual(deviation1, position.AgvPosition.DeviationRange!.Value, 0.00001);

            position = registry.GetPosition(AgvTestIdentifier02.ToGuid());
            Assert.IsNotNull(position);
            Assert.AreEqual(x2, position.AgvPosition.X, 0.00001);
            Assert.AreEqual(y2, position.AgvPosition.Y, 0.00001);
            Assert.AreEqual(z2, position.AgvPosition.Z, 0.00001);
            Assert.AreEqual(theta2, position.AgvPosition.Theta, 0.00001);
            Assert.IsNull(position.AgvPosition.DeviationRange);
        }

        /// <summary>
        /// Test for handling a AgvPositionStatus event
        /// </summary>
        [TestMethod]
        public async Task AgvPositionReceived_Consumed()
        {
            var agvPositionRegistryMock = new Mock<IAgvPositionRegistry>();

            var fixture = new MassTransitTestFixture<AgvPositionReceivedConsumer>(services =>
            {
                services.AddSingleton(agvPositionRegistryMock.Object);
                services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            });

            await fixture.TestHarness.Bus.Publish(
                new AgvPositionReceived(
                    AgvTestIdentifier01.ToGuid(),
                    AgvTestIdentifier01,
                    new AgvPosition(false, 0, 0, 0, 0)
                ));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvPositionReceived>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AgvPositionReceived>());

            agvPositionRegistryMock.Verify(h => h.UpdatePosition(It.Is<AgvPositionStatus>(o => o.AgvId == AgvTestIdentifier01 &&
                                                                                                 o.CorrelationId == AgvTestIdentifier01.ToGuid())), Times.Once);
        }
    }
}
