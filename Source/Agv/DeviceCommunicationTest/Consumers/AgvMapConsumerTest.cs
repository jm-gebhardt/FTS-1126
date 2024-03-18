using System.Globalization;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

/// <summary>
/// RequestAgvMapConsumerTest
/// </summary>
[TestClass]
public class AgvMapConsumerTest
{
    private const string AgvSerialNumber = "Agv01";
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

    private readonly MqttMap _mqttMap = new(AgvSerialNumber, AgvSerialNumber)
    {
        Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=",
        MapId = MapId,
        ModificationDate = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
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
        var fixture = new MassTransitConsumerTestFixture<RequestAgvMapConsumer>();

        // Act
        await fixture.TestHarness.Bus.Publish(new RequestAgvMap(AgvSerialNumber.ToGuid(), AgvSerialNumber, MapId.ToGuid(), MapId));

        // Assert
        (await fixture.TestHarness.Published.Any<RequestAgvMap>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<RequestAgvMap>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<MqttRequestMap>()).ShouldBeTrue();
        var message = fixture.TestHarness.Published.Select<MqttRequestMap>().First().Context.Message;
        message.SerialNumber.ShouldBe(AgvSerialNumber);
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
        (await fixture.TestHarness.Published.Any<MqttMap>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<MqttMap>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvMapReceived>()).ShouldBeTrue();
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
        (await fixture.TestHarness.Published.Any<MqttMap>()).ShouldBeTrue();
        (await fixture.ConsumerHarness.Consumed.Any<MqttMap>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<AgvMapReceived>()).ShouldBeFalse();
    }
}