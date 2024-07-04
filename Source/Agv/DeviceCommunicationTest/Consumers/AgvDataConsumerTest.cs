using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

/// <summary>
/// Tests for <see cref="MqttAgvData"/> and <see cref="AgvDataReceived"/>.
/// </summary>
[TestClass]
public class AgvDataConsumerTest
{
    private const string AgvSerialNumber = "#0815#";

    /// <summary>
    /// Test for consuming invalid AGV data message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveDataMessage_WithInvalidContent_NoMessageSend()
    {
        var fixture = new AgvDataConsumerMqttFixture();

        var message = new MqttAgvData("4711", "", "", null, null, 0.0, 0.0, null, null, null,
            0.0, null, null, null, Array.Empty<Sensor>());

        await fixture.TestHarness.Bus.PublishMqtt(message, null);

        await fixture.ShouldNotHavePublishedAsync<AgvDataReceived>();
    }

    /// <summary>
    /// Test for consuming AGV data message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveDataMessage_WithValidInput_ExpectMessage()
    {
        var fixture = new AgvDataConsumerMqttFixture();
        var sensors = new Sensor[]
        {
            new() { SensorIdentifier = "Back01" },
            new() { SensorIdentifier = "Front01" },
            new() { SensorIdentifier = "Back02" },
            new() { SensorIdentifier = "Front02" }
        };
        var message = new MqttAgvData(AgvSerialNumber, "KARIS", "BF1", "127.0.0.1", "v3.1", 1.8, 120.0, 0.6, 0.8, 0.7, 0.9, 2.5, 1.5, 1000, sensors);
        await fixture.TestHarness.Bus.PublishMqtt(message, null);
        await fixture.ShouldHavePublishedAsync<MqttAgvData>();

        var consumedMessage = fixture.ConsumerHarness.Consumed.Select<MqttAgvData>().First().Context.Message;
        consumedMessage.SerialNumber.ShouldBe(AgvSerialNumber);
        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();

        var publishedData = fixture.TestHarness.Published.Select<AgvDataReceived>().First().Context.Message;
        publishedData.CorrelationId.ShouldBe(AgvSerialNumber.ToGuid());
        publishedData.SerialNumber.ShouldBe(AgvSerialNumber);
        publishedData.Data.Sensors.Length.ShouldBe(4);
        publishedData.Data.Model.ShouldBe("KARIS");
        publishedData.Data.Type.ShouldNotBeNull();
        publishedData.Data.MaxVelocityTurnOnSpot.ShouldBe(2.5);
        publishedData.Data.MaxVelocityStraight.ShouldBe(1.8);
        publishedData.Data.MaxVelocityTurn.ShouldBe(0.9);
        publishedData.Data.Width.ShouldBe(0.8);
        publishedData.Data.Length.ShouldBe(0.6);
        publishedData.Data.Payload.ShouldBe(1000);
        publishedData.Data.Acceleration.ShouldBe(0.7);
        publishedData.Data.IpV4Address.ShouldBe("127.0.0.1");
        publishedData.Data.BatteryCapacity.ShouldBe(120.0);
        publishedData.Data.TurnOnSpotThreshold.ShouldBe(1.5);
        publishedData.Data.SoftwareVersion.ShouldBe("v3.1");
    }
}

public class AgvDataConsumerMqttFixture : MqttConsumerTestFixture<AgvDataConsumer>
{
    public AgvDataConsumerMqttFixture()
    {
        Build();
    }
}