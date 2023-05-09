using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions.Charging
{
    [TestClass]
    public class AgvBatteryChangedConsumerTest
    {
        [TestMethod]
        [DataRow(AgvBatteryChangedConsumerTestFixture.BatteryLevelCritical, AgvBatteryChangedConsumerTestFixture.BatteryLevelCritical)]
        [DataRow(AgvBatteryChangedConsumerTestFixture.BatteryLevelLow, AgvBatteryChangedConsumerTestFixture.BatteryLevelCritical)]
        public async Task Consume_AgvBatteryChargeChanged_publish_AgvNeedsCharging(int oldCharge, int newCharge)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
        
            var fixture = new AgvBatteryChangedConsumerTestFixture();
            await fixture.TestHarness!.Bus.Publish(new AgvBatteryChargeChanged(correlationId, oldCharge, newCharge));
            
            bool consumeCheck = await fixture.ConsumerHarness!.Consumed.Any<AgvBatteryChargeChanged>(s => s.Context.Message.CorrelationId == correlationId);
            consumeCheck.ShouldBeTrue();
        }
        
        [TestMethod]
        [DataRow(AgvBatteryChangedConsumerTestFixture.BatteryLevelCritical, AgvBatteryChangedConsumerTestFixture.BatteryLevelGood)]
        [DataRow(AgvBatteryChangedConsumerTestFixture.BatteryLevelLow, AgvBatteryChangedConsumerTestFixture.BatteryLevelFull)]
        public async Task Consume_AgvBatteryChargeChanged_publish_ChargingAgvsCanHandleActionsAgainEvent(int oldCharge, int newCharge)
        {
            string agvId = "AGV001";
            var correlationId = agvId.ToGuid();
        
            var fixture = new AgvBatteryChangedConsumerTestFixture();
            await fixture.TestHarness!.Bus.Publish(new AgvBatteryChargeChanged(correlationId, oldCharge, newCharge));
        
            bool consumeCheck = await fixture.ConsumerHarness!.Consumed.Any<AgvBatteryChargeChanged>(s => s.Context.Message.CorrelationId == correlationId);
            consumeCheck.ShouldBeTrue();
        
            bool publishCheck = await fixture.TestHarness.Published.Any<ChargingAgvsCanHandleActionsAgainEvent>();
            publishCheck.ShouldBeTrue();
        }
    }
}


