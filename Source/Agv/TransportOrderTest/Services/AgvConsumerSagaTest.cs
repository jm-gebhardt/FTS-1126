using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit.Testing;
using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.Common.Utilities;


namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Tests for AGV consumer saga.
    /// </summary>
    [TestClass]
    public class AgvConsumerSagaTest
    {
        private const string AgvTestIdentifier01 = "#2410#";
        private const string AgvTestIdentifier02 = "#0815#";

        private async Task TestPublishAndResult(
            ConsumerSagaTestFixture<AgvConsumerSaga> fixture,
            object agvStatusReceived,
            string expectedAgv,
            AgvConsumerSaga.AgvState expectedState)
        {
            IEnumerable<ISagaInstance<AgvConsumerSaga>> sagaList;

            await fixture.TestHarness.Bus.Publish<IAgvStatusReceived>(agvStatusReceived);
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<IAgvStatusReceived>());
            var timeout = Task.Delay(5000);
            do
            {
                sagaList = fixture.SagaHarness.Sagas.Select(s => s.AgvId == expectedAgv && s.CurrentState == expectedState).ToList();
                Thread.Sleep(1);
                Assert.IsFalse(timeout.IsCompleted);
            } while (!sagaList.Any());
            Assert.AreEqual(1, sagaList.Count());
            var saga = sagaList.First().Saga;
            Assert.AreEqual(expectedState, saga.CurrentState);
            Assert.AreEqual(expectedAgv, saga.AgvId);
        }

        /// <summary>
        /// Test ok-cases when status message is received.
        /// </summary>
        [TestMethod]
        public async Task When_AgvStatusReceived_AgvStateIsUpdated()
        {
            var fixture = new ConsumerSagaTestFixture<AgvConsumerSaga>();

            // Simulate receive for AGV 1 -> Idle
            await TestPublishAndResult(
                fixture,
                new
                {
                    CorrelationId = AgvTestIdentifier01.ToGuid(),
                    AgvId = AgvTestIdentifier01,
                    Busy = false,
                    Automatic = true,
                    BatteryCharge = 0,
                    Charging = false,
                    Disturbed = false
                },
                AgvTestIdentifier01,
                AgvConsumerSaga.AgvState.Idle
            );

            // Simulate state change for AGV 1 -> Busy
            await TestPublishAndResult(
                fixture,
                new
                {
                    CorrelationId = AgvTestIdentifier01.ToGuid(),
                    AgvId = AgvTestIdentifier01,
                    Busy = true,
                    Automatic = true,
                    BatteryCharge = IAgvStatusReceived.GoodBatteryLevel,
                    Charging = false,
                    Disturbed = false
                },
                AgvTestIdentifier01,
                AgvConsumerSaga.AgvState.Busy
            );

            // Simulate state change for AGV 1 -> BusyRequestForCharging
            await TestPublishAndResult(
                fixture,
                new
                {
                    CorrelationId = AgvTestIdentifier01.ToGuid(),
                    AgvId = AgvTestIdentifier01,
                    Busy = true,
                    Automatic = true,
                    BatteryCharge = IAgvStatusReceived.BadBatteryLevel - 1,
                    Charging = false,
                    Disturbed = false
                },
                AgvTestIdentifier01,
                AgvConsumerSaga.AgvState.BusyRequestForCharging
            );

            // Simulate receive for AGV 2 -> Error
            await TestPublishAndResult(
                fixture,
                new
                {
                    CorrelationId = AgvTestIdentifier02.ToGuid(),
                    AgvId = AgvTestIdentifier02,
                    Busy = false,
                    Automatic = true,
                    BatteryCharge = 0,
                    Charging = false,
                    Disturbed = true
                },
                AgvTestIdentifier02,
                AgvConsumerSaga.AgvState.Error
            );

            // Simulate receive for AGV 2 -> Charging
            await TestPublishAndResult(
                fixture,
                new
                {
                    CorrelationId = AgvTestIdentifier02.ToGuid(),
                    AgvId = AgvTestIdentifier02,
                    Busy = false,
                    Automatic = true,
                    BatteryCharge = IAgvStatusReceived.BadBatteryLevel + 1,
                    Charging = true,
                    Disturbed = false
                },
                AgvTestIdentifier02,
                AgvConsumerSaga.AgvState.Charging
            );

            // Simulate receive for AGV 2 -> Maintenance
            await TestPublishAndResult(
                fixture,
                new
                {
                    CorrelationId = AgvTestIdentifier02.ToGuid(),
                    AgvId = AgvTestIdentifier02,
                    Busy = false,
                    Automatic = false,
                    BatteryCharge = IAgvStatusReceived.BadBatteryLevel + (IAgvStatusReceived.GoodBatteryLevel - IAgvStatusReceived.BadBatteryLevel) / 2,
                    Charging = false,
                    Disturbed = false
                },
                AgvTestIdentifier02,
                AgvConsumerSaga.AgvState.Maintenance
            );

            Assert.AreEqual(2, fixture.SagaHarness.Sagas.Count());
        }
    }
}