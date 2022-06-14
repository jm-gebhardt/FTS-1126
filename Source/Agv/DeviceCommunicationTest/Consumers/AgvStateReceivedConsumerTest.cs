using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit;
using MassTransit.Testing;
using MassTransit.Configuration;
using MassTransit.Serialization;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

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
            public string Agv { get; init; } = string.Empty;
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

            var agvState = new DummyState
            {
                SerialNumber = "AGV123",
                OrderId = "ORDER123",
                ActionStates = actionStates,
                OperatingMode = OperatingMode.Automatic,
                BatteryState = new DummyBatteryState { BatteryCharge = 1.0, Charging = true },
                SafetyState = new DummySafetyState { EStop = EStop.None, FieldViolation = true }
            };

            var expectedReply = new DummyAgvStatusReceived
            {
                Agv = "AGV123",
                OrderId = "ORDER123",
                Busy = true,
                Automatic = true,
                BatteryCharge = 100,
                Charging = true,
                Disturbed = true,
                CorrelationId = Guid.NewGuid()
            };

            await ConsumeMessage(agvState, expectedReply);
        }

        /// <summary>
        /// Test for consuming AGV state message the results in all properties of the reply set to false/0/empty
        /// </summary>
        [TestMethod]
        public async Task ReceiveStateMessageWithAllFalseValues()
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

            var expectedReply = new DummyAgvStatusReceived
            {
                Agv = string.Empty,
                OrderId = string.Empty,
                Busy = false,
                Automatic = false,
                BatteryCharge = 0,
                Charging = false,
                Disturbed = false,
                CorrelationId = Guid.Empty
            };

            await ConsumeMessage(agvState, expectedReply);
        }

        /// <summary>
        /// Test for consuming empty AGV state message
        /// </summary>
        [TestMethod]
        public async Task ConsumeEmptyagvState()
        {
            var agvState = new DummyState();

            var expectedReply = new DummyAgvStatusReceived
            {
                Agv = string.Empty,
                OrderId = null,
                Busy = false,
                Automatic = true, // If not definend explicitly, the value of an enum will default to '0' which in this case is 'OperatingMode.Automatic'
                BatteryCharge = 0,
                Charging = false,
                Disturbed = false
            };

            await ConsumeMessage(agvState, expectedReply);
        }

        /// <summary>
        /// Consume AGV state message and check if the properties of the reply matches the expected values
        /// </summary>
        private async Task ConsumeMessage(DummyState agvState, DummyAgvStatusReceived expectedReply)
        {
            var collection = new ServiceCollection().
                AddMassTransitTestHarness(x =>
                {
                    x.AddMqttConsumer<AgvStateConsumer>();
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.AddSerializer(new SystemTextJsonRawMessageSerializerFactory());
                        cfg.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;
                        cfg.ConfigureEndpoints(context);
                    });
                });

            var provider = collection?.BuildServiceProvider(true);
            var testHarness = provider.GetTestHarness();
            await testHarness.Start();

            IConsumerTestHarness<AgvStateConsumer> consumerHarness = testHarness.GetConsumerHarness<AgvStateConsumer>();
            await testHarness.Bus.PublishMqtt<IState>(agvState, null);

            Assert.IsTrue(await testHarness.Published.Any<IState>());

            var consumedMsg = consumerHarness.Consumed.Select<IState>().First().Context.Message;

            Assert.AreEqual(consumedMsg.SerialNumber, agvState.SerialNumber);
            Assert.AreEqual(consumedMsg.OrderId, agvState.OrderId);

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

            var publishedMsg = testHarness.Published.Select<IAgvStatusReceived>().First().Context.Message;

            Assert.AreEqual(publishedMsg.Agv, expectedReply.Agv);
            Assert.AreEqual(publishedMsg.OrderId, expectedReply.OrderId);
            Assert.AreEqual(publishedMsg.Busy, expectedReply.Busy);
            Assert.AreEqual(publishedMsg.Automatic, expectedReply.Automatic);
            Assert.AreEqual(publishedMsg.BatteryCharge, expectedReply.BatteryCharge);
            Assert.AreEqual(publishedMsg.Charging, expectedReply.Charging);
            Assert.AreEqual(publishedMsg.Disturbed, expectedReply.Disturbed);
        }
    }
}
