using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// Test for AgvStateConsumer
    /// </summary>
    [TestClass]
    public class AgvStateConsumerTest
    {
        #region Dummy Interface Implementations

        private class DummyAgvStatusReceived : IAgvStatusReceived
        {
            public string AgvId { get; init; } = string.Empty;
            public string? OrderId { get; init; }
            public bool Busy { get; init; }
            public bool Automatic { get; init; }
            public int BatteryCharge { get; init; }
            public bool Charging { get; init; }
            public bool Disturbed { get; init; }
            public Guid CorrelationId { get; init; }
        }

        private class DummyState : IState
        {
            public List<IActionState>? ActionStates { get; set; }
            public IAgvPosition? AgvPosition { get; set; }
            public IBatteryState? BatteryState { get; set; }
            public double? DistanceSinceLastNode { get; set; }
            public bool Driving { get; set; }
            public List<IEdgeState>? EdgeStates { get; set; }
            public List<IError>? Errors { get; set; }
            public long HeaderId { get; set; }
            public List<IInformation>? Information { get; set; }
            public string? LastNodeId { get; set; }
            public long LastNodeSequenceId { get; set; }
            public List<ILoad>? Loads { get; set; }
            public string? Manufacturer { get; set; }
            public bool? NewBaseRequest { get; set; }
            public List<INodeState>? NodeStates { get; set; }
            public OperatingMode OperatingMode { get; set; }
            public string? OrderId { get; set; }
            public long OrderUpdateId { get; set; }
            public bool? Paused { get; set; }
            public ISafetyState? SafetyState { get; set; }
            public string? SerialNumber { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public IVelocity? Velocity { get; set; }
            public string? Version { get; set; }
            public string? ZoneSetId { get; set; }
        }

        private class DummyActionState : IActionState
        {
            public string? ActionDescription { get; set; }
            public string? ActionId { get; set; }
            public ActionStatus ActionStatus { get; set; }
            public string? ActionType { get; set; }
            public string? ResultDescription { get; set; }
        }

        private class DummyBatteryState : IBatteryState
        {
            public double BatteryCharge { get; set; }
            public long? BatteryHealth { get; set; }
            public double? BatteryVoltage { get; set; }
            public bool Charging { get; set; }
            public long? Reach { get; set; }
        }

        private class DummySafetyState : ISafetyState
        {
            public EStop EStop { get; set; }
            public bool FieldViolation { get; set; }
        }

        #endregion


        /// <summary>
        /// Test for consuming AGV state message the results in all properties of the reply set to true/100/set
        /// </summary>
        [TestMethod]
        public async Task ReceiveStateMessageWithAllTrueValues()
        {
            var actionStates = new List<IActionState>
            { // ReSharper disable All 
                new DummyActionState { ActionStatus = ActionStatus.Running }
            }; // ReSharper restore All 

            const string serialNumber = "AGV0";
            const string orderId = "ORDER123";
            var agvState = new DummyState
            {
                SerialNumber = serialNumber,
                OrderId = orderId,
                ActionStates = actionStates,
                OperatingMode = OperatingMode.Automatic,
                BatteryState = new DummyBatteryState { BatteryCharge = 1.0, Charging = true },
                SafetyState = new DummySafetyState { EStop = EStop.None, FieldViolation = true }
            };

            var expectedReply = new DummyAgvStatusReceived
            {
                AgvId = serialNumber,
                OrderId = orderId,
                Busy = true,
                Automatic = true,
                BatteryCharge = 100,
                Charging = true,
                Disturbed = true,
                CorrelationId = serialNumber.ToGuid()
            };

            await ConsumeMessage(agvState, expectedReply);
        }

        /// <summary>
        /// Test for consuming AGV state message the results in all properties of the reply set to false/0/empty
        /// </summary>
        [TestMethod]
        public async Task ReceiveStateMessageWithAllFalseValues_SerialNumberSet()
        {
            var actionStates = new List<IActionState>
            { // ReSharper disable All 
                new DummyActionState { ActionStatus = ActionStatus.Failed },
                new DummyActionState { ActionStatus = ActionStatus.Finished },
                new DummyActionState { ActionStatus = ActionStatus.Waiting }
            }; // ReSharper restore All 
            string serialNumber = "AGV0";
            var agvState = new DummyState
            {
                SerialNumber = serialNumber,
                OrderId = string.Empty,
                ActionStates = actionStates,
                OperatingMode = OperatingMode.Manual,
                BatteryState = new DummyBatteryState { BatteryCharge = 0.0, Charging = false },
                SafetyState = new DummySafetyState { EStop = EStop.None, FieldViolation = false }
            };

            var expectedReply = new DummyAgvStatusReceived
            {
                AgvId = serialNumber,
                OrderId = string.Empty,
                Busy = false,
                Automatic = false,
                BatteryCharge = 0,
                Charging = false,
                Disturbed = false,
                CorrelationId = serialNumber.ToGuid()
            };

            await ConsumeMessage(agvState, expectedReply);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task ReceiveStateMessageWithEmptySerialNumber_NoMessageSend()
        {
            var actionStates = new List<IActionState>
            { // ReSharper disable All 
                new DummyActionState { ActionStatus = ActionStatus.Failed },
                new DummyActionState { ActionStatus = ActionStatus.Finished },
                new DummyActionState { ActionStatus = ActionStatus.Waiting }
            }; // ReSharper restore All 

            var agvState = new DummyState
            {
                SerialNumber = string.Empty,
                OrderId = string.Empty,
                ActionStates = actionStates,
                OperatingMode = OperatingMode.Manual,
                BatteryState = new DummyBatteryState { BatteryCharge = 0.0, Charging = false },
                SafetyState = new DummySafetyState { EStop = EStop.None, FieldViolation = false }
            };
            
            await ConsumeMessage(agvState, null);
        }

        /// <summary>
        /// Test for consuming empty AGV state message
        /// </summary>
        [TestMethod]
        public async Task ConsumeEmptyAgvState_SerialNumberSet()
        {
            const string serialNumber = "AGV0";
            var agvState = new DummyState() { SerialNumber = serialNumber};
            var expectedReply = new DummyAgvStatusReceived
            {
                CorrelationId = serialNumber.ToGuid(),
                AgvId = serialNumber,
                OrderId = null,
                Busy = false,
                Automatic = true, // If not defined explicitly, the value of an enum will default to '0' which in this case is 'OperatingMode.Automatic'
                BatteryCharge = 0,
                Charging = false,
                Disturbed = false
            };

            await ConsumeMessage(agvState, expectedReply);
        }

        /// <summary>
        /// Consume AGV state message and check if the properties of the reply matches the expected values
        /// </summary>
        private async Task ConsumeMessage(DummyState agvState, DummyAgvStatusReceived? expectedReply)
        {
            var fixture = new MqttConsumerTestFixture<AgvStateConsumer>();

            await fixture.TestHarness.Bus.PublishMqtt<IState>(agvState, null);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<IState>());

            var consumedMsg = fixture.ConsumerHarness.Consumed.Select<IState>().First().Context.Message;

            Assert.AreEqual(agvState.SerialNumber, consumedMsg.SerialNumber);
            Assert.AreEqual(agvState.OrderId, consumedMsg.OrderId);

            if (agvState.ActionStates == null)
            {
                Assert.IsNull(consumedMsg.ActionStates);
            }
            else
            {
                Assert.IsNotNull(consumedMsg.ActionStates);
                Assert.AreEqual(consumedMsg.ActionStates.Count, agvState.ActionStates.Count);
                for (int i = 0; i < consumedMsg.ActionStates.Count; i++)
                {
                    Assert.AreEqual(consumedMsg.ActionStates[i].ActionStatus, agvState.ActionStates[i].ActionStatus);
                }
            }

            if (agvState.BatteryState == null)
            {
                Assert.IsNull(consumedMsg.BatteryState);
            }
            else
            {
                Assert.IsNotNull(consumedMsg.BatteryState);
                Assert.AreEqual(consumedMsg.BatteryState.BatteryCharge, agvState.BatteryState.BatteryCharge);
                Assert.AreEqual(consumedMsg.BatteryState.Charging, agvState.BatteryState.Charging);
            }

            if (agvState.SafetyState == null)
            {
                Assert.IsNull(consumedMsg.SafetyState);
            }
            else
            {
                Assert.IsNotNull(consumedMsg.SafetyState);
                Assert.AreEqual(consumedMsg.SafetyState.EStop, agvState.SafetyState.EStop);
                Assert.AreEqual(consumedMsg.SafetyState.FieldViolation, agvState.SafetyState.FieldViolation);
            }

            if (expectedReply == null)
            {
                Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvStatusReceived>());
            }
            else
            {
                var publishedMsg = fixture.TestHarness.Published.Select<IAgvStatusReceived>().First().Context.Message;
                Assert.AreEqual(expectedReply.AgvId, publishedMsg.AgvId);
                Assert.AreEqual(expectedReply.OrderId, publishedMsg.OrderId);
                Assert.AreEqual(expectedReply.Busy, publishedMsg.Busy);
                Assert.AreEqual(expectedReply.Automatic, publishedMsg.Automatic);
                Assert.AreEqual(expectedReply.BatteryCharge, publishedMsg.BatteryCharge);
                Assert.AreEqual(expectedReply.Charging, publishedMsg.Charging);
                Assert.AreEqual(expectedReply.Disturbed, publishedMsg.Disturbed);
            }
        }
    }
}
