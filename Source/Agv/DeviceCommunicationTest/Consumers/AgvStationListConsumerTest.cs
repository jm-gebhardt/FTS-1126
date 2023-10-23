using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
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
    private const string AgvTestIdentifier = "#0815#";

    /// <summary>
    /// Test for consuming AGV station list message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveStationListMessage_WithValidInput_ExpectMessage()
    {
        var fixture = new MqttConsumerTestFixture<AgvStationListConsumer>();

        var stations = new Station[]
        {
            new(Function.Drop, "D1", new Position(0, 1, 2), "?"),
            new(Function.Charge, "C1", new Position(3, 4, 5), "?")
        };
        var message = new MqttStationList(AgvTestIdentifier, DateTimeOffset.UtcNow, stations);

        await fixture.TestHarness.Bus.PublishMqtt(message, null);

        // Did the test publish MqttStationList ...
        (await fixture.TestHarness.Published.Any<MqttStationList>()).ShouldBeTrue();
        // ... and has the message been consumed?
        var consumed = fixture.ConsumerHarness.Consumed.Select<MqttStationList>().First().Context.Message;
        consumed.SerialNumber.ShouldBe(AgvTestIdentifier);

        // Did the consumer forward a StationList message ...
        (await fixture.TestHarness.Published.Any<InternalStationListReceived>()).ShouldBeTrue();
        // ... with expected content?
        var published = fixture.TestHarness.Published.Select<InternalStationListReceived>().First().Context.Message;
        published.CorrelationId.ShouldBe(AgvTestIdentifier.ToGuid());
        published.StationList.SerialNumber.ShouldBe(AgvTestIdentifier);
        published.StationList.Stations.Length.ShouldBe(2);
        published.StationList.Stations.Any(x => x.Identifier == "D1").ShouldBeTrue();
        published.StationList.Stations.Any(x => x.Identifier == "C1").ShouldBeTrue();
    }
}