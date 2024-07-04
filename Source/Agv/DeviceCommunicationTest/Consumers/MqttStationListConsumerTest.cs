using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

/// <summary>
/// Tests for <see cref="MqttStationList"/> and <see cref="Station"/>.
/// </summary>
[TestClass]
public class MqttStationListConsumerTest
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
        await fixture.ShouldHavePublishedAsync<MqttStationList>();
        // ... and has the message been consumed?
        var consumed = await fixture.GetFirstConsumedMessageAsync<MqttStationList>();
        consumed.SerialNumber.ShouldBe(AgvSerialNumber);

        // Did the consumer forward a StationList message ...
        await fixture.ShouldHavePublishedAsync<InternalStationListReceived>();
        // ... with expected content?
        var published = await fixture.GetFirstPublishedMessageAsync<InternalStationListReceived>();
        published.CorrelationId.ShouldBe(AgvSerialNumber.ToGuid());
        published.StationList.SerialNumber.ShouldBe(AgvSerialNumber);
        published.StationList.Stations.Length.ShouldBe(2);
        published.StationList.Stations.Any(x => x.Identifier == "D1").ShouldBeTrue();
        published.StationList.Stations.Any(x => x.Identifier == "C1").ShouldBeTrue();
    }
}

public class AgvStationListConsumerFixture : MqttConsumerTestFixture<MqttStationListConsumer>
{
    public AgvStationListConsumerFixture()
    {
        Build();
    }
}