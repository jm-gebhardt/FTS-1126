using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// Tests for <see cref="MqttAgvData"/> and <see cref="AgvDataReceived"/>.
    /// </summary>
    [TestClass]
    public class AgvDataConsumerTest
    {
        private const string AgvTestIdentifier = "#0815#";

        /// <summary>
        /// Test for consuming invalid AGV data message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveDataMessage_WithInvalidContent_NoMessageSend()
        {
            var fixture = new MqttConsumerTestFixture<AgvDataConsumer>();

            var message = new MqttAgvData("", "", "", "", 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0, Array.Empty<Sensor>());

            await fixture.TestHarness.Bus.PublishMqtt(message, null);

            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvDataReceived>());
        }

        /// <summary>
        /// Test for consuming AGV data message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveDataMessage_WithValidInput_ExpectMessage()
        {
            var fixture = new MqttConsumerTestFixture<AgvDataConsumer>();
            var sensors = new Sensor[]
            {
                new() { SensorIdentifier = "Back01" },
                new() { SensorIdentifier = "Front01" },
                new() { SensorIdentifier = "Back02" },
                new() { SensorIdentifier = "Front02" }
            };
            var message = new MqttAgvData(AgvTestIdentifier, "Karis", "BF1", "127.0.0.1", 1.8, 120.0, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, sensors);

            await fixture.TestHarness.Bus.PublishMqtt(message, null);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttAgvData>());

            var consumedMessage = fixture.ConsumerHarness.Consumed.Select<MqttAgvData>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier, consumedMessage.SerialNumber);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvDataReceived>());

            var publishedData = fixture.TestHarness.Published.Select<AgvDataReceived>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier.ToGuid(), publishedData.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier, publishedData.AgvId);
            Assert.AreEqual(4, publishedData.Data.Sensors.Length);
            Assert.AreEqual("Karis", publishedData.Data.Model);
            Assert.IsNotNull(publishedData.Data.Type);
            Assert.IsNotNull(publishedData.Data.MaxVelocityTurnOnSpot);
            Assert.IsNotNull(publishedData.Data.MaxVelocityStraight);
            Assert.IsNotNull(publishedData.Data.MaxVelocityTurn);
            Assert.IsNotNull(publishedData.Data.Width);
            Assert.IsNotNull(publishedData.Data.Length);
            Assert.IsNotNull(publishedData.Data.Payload);
            Assert.IsNotNull(publishedData.Data.Acceleration);
            Assert.IsNotNull(publishedData.Data.IpV4Address);
            Assert.IsNotNull(publishedData.Data.BatteryCapacity);
        }
    }
}
