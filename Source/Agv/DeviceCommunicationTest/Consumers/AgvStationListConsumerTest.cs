using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

/// <summary>
/// Tests for <see cref="MqttStationList"/> and <see cref="Station"/>.
/// </summary>
[TestClass]
public class AgvStationListConsumerTest
{
    private const string AgvSerialNumber = "#0815#";

    /// <summary>
    /// Test for consuming AGV station list message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveStationListMessage_WithValidInput_ExpectMessage()
    {
        var fixture = new AgvStationListConsumerFixture();

        var stations = new Station[]
        {
            new(Function.Drop, "D1", new Position(0, 1, 2), "?"),
            new(Function.Charge, "C1", new Position(3, 4, 5), "?")
        };
        var message = new MqttStationList(AgvSerialNumber, DateTimeOffset.UtcNow, stations);

        await fixture.TestHarness.Bus.PublishMqtt(message, null);

        // Did the test publish MqttStationList ...
        (await fixture.TestHarness.Published.Any<MqttStationList>()).ShouldBeTrue();
        // ... and has the message been consumed?
        var consumed = fixture.ConsumerHarness.Consumed.Select<MqttStationList>().First().Context.Message;
        consumed.SerialNumber.ShouldBe(AgvSerialNumber);

        // Did the consumer forward a StationList message ...
        (await fixture.TestHarness.Published.Any<InternalStationListReceived>()).ShouldBeTrue();
        // ... with expected content?
        var published = fixture.TestHarness.Published.Select<InternalStationListReceived>().First().Context.Message;
        published.CorrelationId.ShouldBe(AgvSerialNumber.ToGuid());
        published.StationList.SerialNumber.ShouldBe(AgvSerialNumber);
        published.StationList.Stations.Length.ShouldBe(2);
        published.StationList.Stations.Any(x => x.Identifier == "D1").ShouldBeTrue();
        published.StationList.Stations.Any(x => x.Identifier == "C1").ShouldBeTrue();
    }
}

public class AgvStationListConsumerFixture : MqttConsumerTestFixture<AgvStationListConsumer>
{
    public AgvStationListConsumerFixture()
    {
        Build();
    }
}