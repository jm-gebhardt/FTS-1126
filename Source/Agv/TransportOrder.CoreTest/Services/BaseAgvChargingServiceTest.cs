using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Extensions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    /// <summary>
    /// Tests for <see cref="BaseAgvChargingServiceTest"/>
    /// </summary>
    [TestClass]
    public class BaseAgvChargingServiceTest
    {
        #region Setup
        private const int BatteryLevelCritical = 10;
        private const int BatteryLevelLow = 40;
        private const int BatteryLevelGood = 80;
        private const int BatteryLevelFull = 100;
        private static readonly AgvSettings _agvSettings = new()
        {
            BatteryLevelCritical = BatteryLevelCritical,
            BatteryLevelFull = BatteryLevelFull,
            BatteryLevelGood = BatteryLevelGood,
            BatteryLevelLow = BatteryLevelLow,
            AgvOfflineTimeoutMsWhileIdle = 15000,
            AgvOfflineTimeoutMsWhileBusy = 20000
        };

        private BaseAgvChargingService GetBaseAgvChargingService(int firstAgvBatteryCharge = BatteryLevelLow)
        {
            var logger = new Mock<ILogger<BaseAgvChargingService>>();
            Mock<IAgvRegistry> agvRegistry = AgvRegistryMock.GetAgvRegistry();
            InitAgvRegistryMock(agvRegistry, firstAgvBatteryCharge);

            var settingsProviderMock = new Mock<IAgvSettingsProvider>();
            settingsProviderMock.Setup(m => m.GetSettingsForAgv(It.IsAny<Guid>())).Returns(() => _agvSettings);

            return new BaseAgvChargingService(logger.Object, agvRegistry.Object, settingsProviderMock.Object);
        }

        private IAgv CreateAgv(string agvId, int batteryCharge)
        {
            var correlationId = agvId.ToGuid();
            var agvStatus = new AgvStatus(correlationId, "", true, true, batteryCharge, false, false);
            return new AgvSaga(correlationId, agvId, "Initial", agvStatus);
        }

        private MassTransitTestFixture<BaseAgvChargingService> GetTestFixture(int firstAgvBatteryCharge = BatteryLevelLow)
        {
            var logger = new Mock<ILogger<BaseAgvChargingService>>();
            Mock<IAgvRegistry> agvRegistry = AgvRegistryMock.GetAgvRegistry();
            InitAgvRegistryMock(agvRegistry, firstAgvBatteryCharge);

            var fixture = new MassTransitTestFixture<BaseAgvChargingService>(services =>
            {
                services.AddTransient(_ => logger);
                services.AddTransient(_ => agvRegistry.Object);
                services.AddAgvSettings(_agvSettings);
            });

            return fixture;
        }

        private void InitAgvRegistryMock(Mock<IAgvRegistry> agvRegistry, int firstAgvBatteryCharge)
        {
            var agv1 = agvRegistry.Object.GetAgv("AGV001".ToGuid()) as AgvSaga;
            SetBatteryCharge(agv1!, firstAgvBatteryCharge);

            var agv2 = agvRegistry.Object.GetAgv("AGV002".ToGuid()) as AgvSaga;
            SetBatteryCharge(agv2!, BatteryLevelGood);

            var agv3 = agvRegistry.Object.GetAgv("AGV003".ToGuid()) as AgvSaga;
            agv3!.AgvStatus = null; // Test case for AGV without status
        }

        private void SetBatteryCharge(AgvSaga agv, int batteryCharge)
        {
            if (agv.AgvStatus != null)
            {
                agv.AgvStatus = new AgvStatus(
                   agv.AgvStatus.CorrelationId,
                   agv.AgvStatus.OrderId,
                   agv.AgvStatus.Busy,
                   agv.AgvStatus.Automatic,
                   batteryCharge,
                   agv.AgvStatus.Charging,
                   agv.AgvStatus.Disturbed,
                   agv.AgvStatus.LastNodeId);
            }
        }
        #endregion

        [TestMethod]
        [DataRow(BatteryLevelCritical, BatteryLevelCritical)]
        [DataRow(BatteryLevelLow, BatteryLevelLow)]
        [DataRow(BatteryLevelGood, BatteryLevelGood)]
        [DataRow(BatteryLevelFull, BatteryLevelFull)]
        public async Task Consume_AgvBatteryChargeChanged_publish_no_message(int oldCharge, int newCharge)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();

            MassTransitTestFixture<BaseAgvChargingService> fixture = GetTestFixture(oldCharge);
            await fixture.TestHarness!.Bus.Publish(new AgvBatteryChargeChanged(correlationId, oldCharge, newCharge));

            int publishedCount = fixture.TestHarness.Published.Count();
            publishedCount.ShouldBe(1);

            bool consumeCheck = await fixture.ConsumerHarness!.Consumed.Any<AgvBatteryChargeChanged>(s => s.Context.Message.CorrelationId == correlationId);
            consumeCheck.ShouldBeTrue();

            // No additional message should be published
            fixture.TestHarness.Published.Count().ShouldBe(publishedCount);
        }

        [TestMethod]
        [DataRow(BatteryLevelCritical, BatteryLevelGood)]
        [DataRow(BatteryLevelLow, BatteryLevelFull)]
        public async Task Consume_AgvBatteryChargeChanged_publish_ChargingAgvsCanHandleActionsAgainEvent(int oldCharge, int newCharge)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();

            MassTransitTestFixture<BaseAgvChargingService> fixture = GetTestFixture(oldCharge);
            await fixture.TestHarness!.Bus.Publish(new AgvBatteryChargeChanged(correlationId, oldCharge, newCharge));

            bool consumeCheck = await fixture.ConsumerHarness!.Consumed.Any<AgvBatteryChargeChanged>(s => s.Context.Message.CorrelationId == correlationId);
            consumeCheck.ShouldBeTrue();

            bool publishCheck = await fixture.TestHarness.Published.Any<ChargingAgvsCanHandleActionsAgainEvent>();
            publishCheck.ShouldBeTrue();
        }

        [TestMethod]
        [DataRow(BatteryLevelCritical, true)]
        [DataRow(BatteryLevelLow, false)]
        [DataRow(BatteryLevelGood, false)]
        [DataRow(BatteryLevelFull, false)]
        public void Test_AgvNeedsCharging(int batteryCharge, bool needsCharging)
        {
            var baseAgvChargingService = GetBaseAgvChargingService();
            var agv = CreateAgv("AGV001", batteryCharge);
            baseAgvChargingService.AgvNeedsCharging(agv).ShouldBe(needsCharging);
        }

        [TestMethod]
        [DataRow(BatteryLevelCritical, true)]
        [DataRow(BatteryLevelLow, true)]
        [DataRow(BatteryLevelGood, false)]
        [DataRow(BatteryLevelFull, false)]
        public void Test_AgvIsEligibleForCharging(int batteryCharge, bool needsCharging)
        {
            var baseAgvChargingService = GetBaseAgvChargingService();
            var agv = CreateAgv("AGV001", batteryCharge);
            baseAgvChargingService.AgvIsEligibleForCharging(agv).ShouldBe(needsCharging);
        }

        [TestMethod]
        [DataRow(BatteryLevelCritical, new[] { "AGV002" })]
        [DataRow(BatteryLevelLow, new[] { "AGV002" })]
        [DataRow(BatteryLevelGood, new[] { "AGV001", "AGV002" })]
        [DataRow(BatteryLevelFull, new[] { "AGV001", "AGV002" })]
        public void Test_GetChargingAgvsWithGoodBatteryLevel(int firstAgvBatteryCharge, string[] chargedAgvs)
        {
            var baseAgvChargingService = GetBaseAgvChargingService(firstAgvBatteryCharge);
            List<IAgv> agvList = baseAgvChargingService.GetChargingAgvsWithGoodBatteryLevel();
            agvList.Count.ShouldBe(chargedAgvs.Length);

            for (int i = 0; i < agvList.Count; i++)
            {
                agvList[i].AgvId.ShouldBe(chargedAgvs[i]);
            }
        }

        [TestMethod]
        public void Test_NotifyChargingAgvsCanHandleActionsAgain()
        {
            var publishEndpointMock = new Mock<IPublishEndpoint>();
            var baseAgvChargingService = GetBaseAgvChargingService();

            baseAgvChargingService.NotifyChargingAgvsCanHandleActionsAgain(publishEndpointMock.Object);
            publishEndpointMock.Verify(b => b.Publish(It.IsAny<ChargingAgvsCanHandleActionsAgainEvent>(), default), Times.Once);
        }
    }
}
