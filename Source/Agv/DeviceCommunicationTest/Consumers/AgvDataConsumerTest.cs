using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Shouldly;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Gebhardt.Acc.DeviceCommunication.Common.Configuration;

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
        var fixture = new AgvDataConsumerMqttFixture().Build();

        var message = new MqttAgvData("4711", "", "", null, null, 0.0, 0.0, null, null, null,
            0.0, null, null, null, []);

        await fixture.TestHarness.Bus.PublishMqtt(message, callback: null);

        await fixture.ShouldNotHavePublishedAsync<AgvDataReceived>();
        await fixture.ShouldNotHavePublishedAsync<AgvInstantActionDefinitionReceived>();
    }

    /// <summary>
    /// Test for consuming AGV data message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveDataMessage_WithValidInput_ExpectMessage()
    {
        var fixture = new AgvDataConsumerMqttFixture().Build();
        var sensors = new Sensor[]
        {
            new() { SensorIdentifier = "Back01" },
            new() { SensorIdentifier = "Front01" },
            new() { SensorIdentifier = "Back02" },
            new() { SensorIdentifier = "Front02" }
        };
        var message = new MqttAgvData(AgvSerialNumber, "KARIS", "BF1", "127.0.0.1", "v3.1", 1.8, 120.0, 0.6, 0.8, 0.7, 0.9, 2.5, 1.5, 1000, sensors);
        await fixture.TestHarness.Bus.PublishMqtt(message, callback: null);
        await fixture.ShouldHaveConsumedAsync<MqttAgvData>();

        var publishedData = await fixture.GetFirstPublishedMessageAsync<AgvDataReceived>();
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

    [TestMethod]
    public async Task ReceiveDataMessage_WithInstantActionDefinition_PublishesAgvInstantActionDefinitionReceived()
    {
        const string model = "KARIS";
        const string type = "BF1";
        const string action = "ActionType";
        var fixture = new AgvDataConsumerMqttFixture()
            .AddInstantActionDefinition(model, type, action).Build();
        var message = new MqttAgvData(AgvSerialNumber, model, type, "127.0.0.1", "v3.1", 1.8, 120.0, 0.6, 0.8, 0.7,
            0.9, 2.5, 1.5, 1000, []);
        await fixture.TestHarness.Bus.PublishMqtt(message, callback: null);
        await fixture.ShouldHavePublishedAsync<MqttAgvData>();

        var consumedMessage = await fixture.GetFirstConsumedMessageAsync<MqttAgvData>();
        consumedMessage.SerialNumber.ShouldBe(AgvSerialNumber);
        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();
        await fixture.ShouldHavePublishedAsync<AgvInstantActionDefinitionReceived>();

        var publishedData = fixture.TestHarness.Published.Select<AgvInstantActionDefinitionReceived>().First().Context.Message;
        publishedData.AgvCorrelationId.ShouldBe(AgvSerialNumber.ToGuid());
        publishedData.InstantActionDefinition.ActionType.ShouldBe(action);
    }
}

public class AgvDataConsumerMqttFixture : MqttConsumerTestFixture<AgvDataConsumer>
{
    private readonly StringBuilder _configBuilder = new();

    public AgvDataConsumerMqttFixture()
    {
        _configBuilder.AppendLine("InstantActions:");
    }

    private class ConfigReader(StringReader stringReader) : IConfigurationReader
    {
        public TextReader Read()
        {
            return stringReader;
        }
    }

    public AgvDataConsumerMqttFixture AddInstantActionDefinition(string model, string type, string action)
    {
        _configBuilder.AppendLine($"  - ActionType: {action}");
        _configBuilder.AppendLine("    Description: A description.");
        _configBuilder.AppendLine("    ResultDescription: A result description");
        _configBuilder.AppendLine("    AgvTypes:");
        _configBuilder.AppendLine($"      - Type: {type}");
        _configBuilder.AppendLine($"        Model: {model}");
        _configBuilder.AppendLine("        ExcludedAgvSerialNumbers: null");
        _configBuilder.AppendLine("    AgvSerialNumbers: null");
        _configBuilder.AppendLine();
        return this;
    }

    public new AgvDataConsumerMqttFixture Build()
    {
        string config = _configBuilder.ToString();
        AddServices(sc => sc.AddSingleton<IConfigurationReader, ConfigReader>(_ => new ConfigReader(new StringReader(config))));
        base.Build();
        return this;
    }
}