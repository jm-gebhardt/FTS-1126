using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// RequestAgvMapConsumerTest
    /// </summary>
    [TestClass]
    public class AgvMapConsumerTest
    {
        private const string AgvId = "Agv01";
        private const string MapId = "baseMap";

        /// <summary/>
        public enum FaultyParameter
        {
            /// <summary/>
            EmptySerialNumber,
            /// <summary/>
            NullMapId,
            /// <summary/>
            NullData
        }

        private readonly MqttMap _mqttMap = new(serialNumber: AgvId, requestId: AgvId)
        {
            Data = "bGlnaHQgdw==",
            MapId = MapId,
            ModificationDate = DateTime.Now.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            Resolution = 1,
            XOffset = 1,
            YOffset = 1
        };

        /// <summary>
        /// Checks if MqttRequest is published after consuming RequestAgvMap
        /// </summary>
        [TestMethod]
        public async Task Consume_RequestAgvMap_and_Publish_MqttRequestMap()
        {
            // Arrange
            var fixture = new MassTransitTestFixture<RequestAgvMapConsumer>();

            // Act
            await fixture.TestHarness.Bus.Publish(new RequestAgvMap(AgvId.ToGuid(), AgvId, MapId.ToGuid(), MapId));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvMap>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<RequestAgvMap>());

            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttRequestMap>());
            var message = fixture.TestHarness.Published.Select<MqttRequestMap>().First().Context.Message;
            Assert.AreEqual(AgvId, message.SerialNumber);
        }

        /// <summary>
        /// Checks if AgvMapReceived is published after consuming valid MqttMap
        /// </summary>
        [TestMethod]
        public async Task Consume_MqttMap_and_Publish_AgvMapReceived()
        {
            // Arrange
            var fixture = new MqttConsumerTestFixture<MqttMapConsumer>();

            // Act
            await fixture.TestHarness.Bus.Publish(_mqttMap);

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttMap>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<MqttMap>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvMapReceived>());
        }

        /// <summary>
        /// Checks if AgvMapReceived is not published after consuming invalid MqttMap
        /// </summary>
        /// <remarks>Covering each type of invalid incoming data</remarks>
        [DataRow(FaultyParameter.EmptySerialNumber)]
        [DataRow(FaultyParameter.NullData)]
        [DataRow(FaultyParameter.NullMapId)]
        [TestMethod]
        public async Task Consume_MqttMap_with_SerialNumber_equaling_NullOrEmpty(FaultyParameter faultyParameter)
        {
            switch (faultyParameter)
            {
                // Arrange
                case FaultyParameter.EmptySerialNumber:
                    _mqttMap.SerialNumber = string.Empty;
                    break;

                case FaultyParameter.NullData:
                    _mqttMap.Data = null;
                    break;

                case FaultyParameter.NullMapId:
                    _mqttMap.MapId = null;
                    break;
            }

            var fixture = new MqttConsumerTestFixture<MqttMapConsumer>();
            
            // Act
            await fixture.TestHarness.Bus.Publish(_mqttMap);

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttMap>());
            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<MqttMap>());

            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvMapReceived>());
        }
    }
}
