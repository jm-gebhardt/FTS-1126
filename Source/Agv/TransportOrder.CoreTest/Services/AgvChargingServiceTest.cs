using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit.Testing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    /// <summary>
    /// Tests for <see cref="AgvChargingService"/>
    /// </summary>
    [TestClass]
    public class AgvChargingServiceTest
    {
        #region Setup
        private MassTransitTestFixture<AgvChargingService> GetTestFixture(int batteryCharge, int countQueuedOrders = 0)
        {
            var logger = new Mock<ILogger<AgvChargingService>>();
            Mock<IAgvRegistry> agvRegistry = AgvRegistryMock.GetAgvRegistry();

            var agv1 = agvRegistry.Object.GetAgv("AGV001".ToGuid()) as AgvSaga;
            agv1!.AgvStatus = new AgvStatus(agv1.CorrelationId, "", true, true, batteryCharge, false, false);

            var agv2 = agvRegistry.Object.GetAgv("AGV002".ToGuid()) as AgvSaga;
            agv2!.AgvStatus = null; // Test case for AGV without status

            var orderRegistryMock = new Mock<IOrderRegistry>();
            orderRegistryMock.SetupGet(s => s.CountQueued).Returns(countQueuedOrders);

            var fixture = new MassTransitTestFixture<AgvChargingService>(services =>
            {
                services.AddTransient(_ => logger.Object);
                services.AddTransient(_ => agvRegistry.Object);
                services.AddTransient(_ => orderRegistryMock.Object);
            });

            return fixture;
        }

        #endregion

        [TestMethod]
        public async Task Check_AGV_that_does_not_exist_no_message_published()
        {
            string agvId = "INVALID";
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelGood;
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge);

            await fixture.TestHarness!.Bus.Publish(new AgvBecameIdle(correlationId));

            int publishedCount = fixture.TestHarness.Published.Count();
            publishedCount.ShouldBe(1);

            bool consumed = await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>(s => s.Context.Message.CorrelationId == correlationId);
            consumed.ShouldBeTrue();

            // No additional message should be published
            fixture.TestHarness.Published.Count().ShouldBe(publishedCount);
        }

        [TestMethod]
        public async Task Check_AGV_without_status_no_message_published()
        {
            string agvId = "AGV002"; // AGV002 is has no status
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelGood;
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge);

            await fixture.TestHarness!.Bus.Publish(new AgvBecameIdle(correlationId));

            int publishedCount = fixture.TestHarness.Published.Count();
            publishedCount.ShouldBe(1);

            bool consumed = await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>(s => s.Context.Message.CorrelationId == correlationId);
            consumed.ShouldBeTrue();

            // No additional message should be published
            fixture.TestHarness.Published.Count().ShouldBe(publishedCount);
        }

        [TestMethod]
        public async Task Check_AGV_with_critical_battery_level_publish_AgvNeedsCharging()
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelCritical;
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge);

            await fixture.TestHarness!.Bus.Publish(new AgvBecameIdle(correlationId));

            bool consumed = await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>(s => s.Context.Message.CorrelationId == correlationId);
            consumed.ShouldBeTrue();

            bool published = await fixture.TestHarness.Published.Any<AgvNeedsCharging>(s => s.Context.Message.CorrelationId == correlationId);
            published.ShouldBeTrue();
        }

        [TestMethod]
        public async Task Check_AGV_with_low_battery_level_publish_AgvRequestsCharging()
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelLow;
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge);

            await fixture.TestHarness!.Bus.Publish(new AgvBecameIdle(correlationId));

            bool consumed = await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>(s => s.Context.Message.CorrelationId == correlationId);
            consumed.ShouldBeTrue();

            bool published = await fixture.TestHarness.Published.Any<AgvRequestsCharging>(s => s.Context.Message.CorrelationId == correlationId);
            published.ShouldBeTrue();
        }

        [TestMethod]
        public async Task Check_AGV_with_good_battery_level_publish_AgvReady()
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelGood;
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge);

            await fixture.TestHarness!.Bus.Publish(new AgvBecameIdle(correlationId));

            bool consumed = await fixture.ConsumerHarness!.Consumed.Any<AgvBecameIdle>(s => s.Context.Message.CorrelationId == correlationId);
            consumed.ShouldBeTrue();

            bool published = await fixture.TestHarness.Published.Any<AgvReady>(s => s.Context.Message.CorrelationId == correlationId);
            published.ShouldBeTrue();
        }

        [TestMethod]
        [DataRow(true, AgvChargingService.BatteryLevelGood, 1)]
        [DataRow(true, AgvChargingService.BatteryLevelFull, 0)]
        public async Task Check_AGV_charged_enough_publish_AgvReady(bool charging, int batteryCharge, int countQueuedOrders)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge, countQueuedOrders);

            var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);
            await fixture.TestHarness!.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

            bool consumed = await fixture.ConsumerHarness!.Consumed.Any<AgvStatusReceived>(s => s.Context.Message.CorrelationId == correlationId);
            consumed.ShouldBeTrue();

            bool published = await fixture.TestHarness.Published.Any<AgvStoppedCharging>(s => s.Context.Message.CorrelationId == correlationId);
            published.ShouldBeTrue();
        }

        [TestMethod]
        public async Task Check_AGV_not_charged_enough_no_message_published()
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelCritical;
            bool charging = true; // charging
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge);

            var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);
            await fixture.TestHarness!.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

            int publishedCount = fixture.TestHarness.Published.Count();
            publishedCount.ShouldBe(1);

            bool consumed = await fixture.ConsumerHarness!.Consumed.Any<AgvStatusReceived>(s => s.Context.Message.CorrelationId == correlationId);
            consumed.ShouldBeTrue();

            // No additional message should be published
            fixture.TestHarness.Published.Count().ShouldBe(publishedCount);
        }

        [TestMethod]
        public async Task Check_AGV_not_charging_no_message_published()
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelGood;
            bool charging = false; // Not charging
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge);

            var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);
            await fixture.TestHarness!.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

            int publishedCount = fixture.TestHarness.Published.Count();
            publishedCount.ShouldBe(1);

            bool consumed = await fixture.ConsumerHarness!.Consumed.Any<AgvStatusReceived>(s => s.Context.Message.CorrelationId == correlationId);
            consumed.ShouldBeTrue();

            // No additional message should be published
            fixture.TestHarness.Published.Count().ShouldBe(publishedCount);
        }
    }
}
