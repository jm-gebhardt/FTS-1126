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
    /// Tests for <see cref="IAgvData"/> and <see cref="AgvDataReceived"/>.
    /// </summary>
    [TestClass]
    public class AgvDataConsumerTest
    {
        private const string AgvTestIdentifier02 = "#0815#";


        /// <summary>
        /// Test for consuming invalid AGV data message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveDataMessage_WithInvalidContent_NoMessageSend()
        {
            var fixture = new MqttConsumerTestFixture<AgvDataConsumer>();

            await fixture.TestHarness.Bus.PublishMqtt<IAgvData>(
                new
                {
                    SerialNumber = "",
                    ModificationDate = DateTimeOffset.UtcNow
                }, null);

            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvDataReceived>());
        }

        /// <summary>
        /// Test for consuming AGV data message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveDataMessage_WithValidInput_ExpectMessage()
        {
            var fixture = new MqttConsumerTestFixture<AgvDataConsumer>();

            await fixture.TestHarness.Bus.PublishMqtt<IAgvData>(
                new
                {
                    SerialNumber = AgvTestIdentifier02,
                    Acceleration = 0.7,
                    BatteryCapacity = 120.0,
                    IpV4Address = "127.0.0.1",
                    Length = 0.6,
                    Width = 0.8,
                    MaxVelocityStraight = 1.8,
                    MaxVelocityTurn = 0.9,
                    MaxVelocityTurnOnSpot = 2.5,
                    Model = "Karis",
                    Type = "BF1",
                    Payload = 1000,
                    Sensors = new[]
                    {
	                    new Sensor() { SensorIdentifier = "Back01"},
	                    new Sensor() { SensorIdentifier = "Front01" },
	                    new Sensor() { SensorIdentifier = "Back02"},
	                    new Sensor() { SensorIdentifier = "Front02" }
                    }
                }, null);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IAgvData>());

            var consumedData = fixture.ConsumerHarness.Consumed.Select<IAgvData>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier02, consumedData.SerialNumber);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvDataReceived>());

            var publishedData = fixture.TestHarness.Published.Select<AgvDataReceived>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier02.ToGuid(), publishedData.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier02, publishedData.AgvId);
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
