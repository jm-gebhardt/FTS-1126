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
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    /// <summary>
    /// Tests for <see cref="AgvChargingService"/>
    /// </summary>
    [TestClass]
    public class AgvChargingServiceTest
    {
        #region Setup
        private MassTransitTestFixture<AgvChargingService> GetTestFixture(int batteryCharge, int countQueuedOrders)
        {
            var logger = new Mock<ILogger<AgvChargingService>>();
            Mock<IAgvRegistry> agvRegistry = AgvRegistryMock.GetAgvRegistry();

            var agv1 = agvRegistry.Object.GetAgv("AGV001".ToGuid()) as AgvSaga;
            if (agv1!.AgvStatus != null)
            {
                agv1.AgvStatus = new AgvStatus(
                agv1.AgvStatus.CorrelationId,
                    agv1.AgvStatus.OrderId,
                    agv1.AgvStatus.Busy,
                    agv1.AgvStatus.Automatic,
                    batteryCharge,
                    agv1.AgvStatus.Charging,
                    agv1.AgvStatus.Disturbed,
                    agv1.AgvStatus.LastNodeId);
            }
            agv1.CurrentState = nameof(AgvStateMachine.Charging);

            var agv2 = agvRegistry.Object.GetAgv("AGV002".ToGuid()) as AgvSaga;
            agv2!.AgvStatus = null; // Test case for AGV without status

            var agv3 = agvRegistry.Object.GetAgv("AGV003".ToGuid()) as AgvSaga;
            agv3!.AgvGraph = null; // Test case for AGV without graph

            var orderRegistryMock = new Mock<IOrderRegistry>();
            orderRegistryMock.SetupGet(s => s.CountQueued).Returns(countQueuedOrders);

            var graphRegistry = new Mock<IGraphRegistry>();
            graphRegistry.Setup(s => s.ReserveNode(It.IsAny<string>())).Returns(true);

            var fixture = new MassTransitTestFixture<AgvChargingService>(services =>
            {
                services.AddTransient(_ => logger);
                services.AddTransient(_ => agvRegistry.Object);
                services.AddTransient(_ => orderRegistryMock.Object);
                services.AddSingleton(_ => graphRegistry.Object);
            });

            return fixture;
        }

        private async Task TestConsumeRespond<TConsumed, TResponse>(TConsumed consumed, int batteryCharge, int countQueuedOrders = 0)
            where TConsumed : class, CorrelatedBy<Guid>
            where TResponse : class, CorrelatedBy<Guid>
        {
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge, countQueuedOrders);

            await fixture.TestHarness!.Bus.Publish(consumed);

            bool consumeCheck = await fixture.ConsumerHarness!.Consumed.Any<TConsumed>(s => s.Context.Message.CorrelationId == consumed.CorrelationId);
            consumeCheck.ShouldBeTrue($"Message of type '{typeof(TConsumed).Name}' was not consumed");

            bool publishCheck = await fixture.TestHarness.Published.Any<TResponse>(s => s.Context.Message.CorrelationId == consumed.CorrelationId);
            publishCheck.ShouldBeTrue($"Message of type '{typeof(TResponse).Name}' was not published");
        }

        private async Task TestConsumeNoAction<TConsumed>(TConsumed consumed, int batteryCharge, int countQueuedOrders = 0)
            where TConsumed : class, CorrelatedBy<Guid>
        {
            MassTransitTestFixture<AgvChargingService> fixture = GetTestFixture(batteryCharge, countQueuedOrders);

            await fixture.TestHarness!.Bus.Publish(consumed);

            int publishedCount = fixture.TestHarness.Published.Count();
            publishedCount.ShouldBe(1);

            bool consumeCheck = await fixture.ConsumerHarness!.Consumed.Any<TConsumed>(s => s.Context.Message.CorrelationId == consumed.CorrelationId);
            consumeCheck.ShouldBeTrue();

            // No additional message should be published
            fixture.TestHarness.Published.Count().ShouldBe(publishedCount);
        }
        #endregion

        #region Consume AgvBecameIdle
        [TestMethod]
        public async Task Consume_AgvBecameIdle_publish_no_message_for_AGV_that_does_not_exist()
        {
            string agvId = "INVALID";
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelGood;
            await TestConsumeNoAction(new AgvBecameIdle(correlationId), batteryCharge);
        }

        [TestMethod]
        public async Task Consume_AgvBecameIdle_publish_no_message_for_AGV_without_status()
        {
            string agvId = "AGV002"; // AGV002 is has no status
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelGood;

            await TestConsumeNoAction(new AgvBecameIdle(correlationId), batteryCharge);
        }

        [TestMethod]
        public async Task Consume_AgvBecameIdle_publish_no_message_for_AGV_without_graph()
        {
            string agvId = "AGV003"; // AGV003 is has no graph
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelGood;

            await TestConsumeNoAction(new AgvBecameIdle(correlationId), batteryCharge);
        }

        [TestMethod]
        [DataRow(AgvChargingService.BatteryLevelFull, 0)]
        [DataRow(AgvChargingService.BatteryLevelGood, 1)]
        public async Task Consume_AgvBecameIdle_publish_AgvReady_for_AGV_charged_enough(int batteryCharge, int countQueuedOrders)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();

            await TestConsumeRespond<AgvBecameIdle, AgvReady>(new AgvBecameIdle(correlationId), batteryCharge, countQueuedOrders);
        }

        [TestMethod]
        [DataRow(AgvChargingService.BatteryLevelCritical, 1)]
        [DataRow(AgvChargingService.BatteryLevelLow, 0)]
        public async Task Consume_AgvBecameIdle_publish_StartActionCommand_for_AGV_needs_charging(int batteryCharge, int countQueuedOrders)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();

            await TestConsumeRespond<AgvBecameIdle, StartActionCommand>(new AgvBecameIdle(correlationId), batteryCharge, countQueuedOrders);
        }
        #endregion

        #region Consume AgvStatusReceived
        [TestMethod]
        public async Task Consume_AgvStatusReceived_publish_no_message_for_AGV_not_charged_enough()
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            int batteryCharge = AgvChargingService.BatteryLevelCritical;
            bool charging = true;
            var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);

            await TestConsumeNoAction(new AgvStatusReceived(correlationId, agvId, agvStatus), batteryCharge);
        }

        [TestMethod]
        [DataRow(AgvChargingService.BatteryLevelFull, 0)]
        [DataRow(AgvChargingService.BatteryLevelGood, 1)]
        public async Task Consume_AgvStatusReceived_publish_AgvStoppedCharging_for_AGV_charged_enough(int batteryCharge, int countQueuedOrders)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            bool charging = true;
            var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);

            await TestConsumeRespond<AgvStatusReceived, AgvStoppedCharging>(new AgvStatusReceived(correlationId, agvId, agvStatus), batteryCharge, countQueuedOrders);
        }

        [TestMethod]
        [DataRow(AgvChargingService.BatteryLevelCritical, 0)]
        [DataRow(AgvChargingService.BatteryLevelLow, 0)]
        public async Task Consume_AgvStatusReceived_publish_AgvNeedsCharging_for_AGV_not_charging(int batteryCharge, int countQueuedOrders)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
            bool charging = false;
            var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, charging, false);

            await TestConsumeRespond<AgvStatusReceived, AgvNeedsCharging>(new AgvStatusReceived(correlationId, agvId, agvStatus), batteryCharge, countQueuedOrders);
        }
        #endregion
    }
}
