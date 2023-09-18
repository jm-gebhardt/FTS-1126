using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

/// <summary>
/// Tests for <see cref="MqttGraph"/> and <see cref="InternalGraphReceived"/>.
/// </summary>
[TestClass]
public class AgvGraphConsumerTest
{
    private const string AgvTestIdentifier = "#0815#";
        
    /// <summary>
    /// Test for consuming AGV graph message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveGraphMessage_WithValidInput_ExpectMessage()
    {
        var fixture = new AgvGraphConsumerTestFixture()
            .UseDefaultServices()
            .Build();
        var nodes = new MqttNode[] {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
        };
        var message = new MqttGraph(AgvTestIdentifier, DateTimeOffset.UtcNow, nodes, edges, string.Empty);

        await fixture.TestHarness.Bus.PublishMqtt(message, null);

        // Did the test publish MqttGraph ...
        (await fixture.TestHarness.Published.Any<MqttGraph>()).ShouldBeTrue();
        // ... and has the message been consumed?
        var consumed = fixture.ConsumerHarness.Consumed.Select<MqttGraph>().First().Context.Message;
        consumed.SerialNumber.ShouldBe(AgvTestIdentifier);

        // Did the consumer forward a Graph message ...
        (await fixture.TestHarness.Published.Any<InternalGraphReceived>()).ShouldBeTrue();
        // ... with expected content?
        var published = fixture.TestHarness.Published.Select<InternalGraphReceived>().First().Context.Message;
        published.CorrelationId.ShouldBe(AgvTestIdentifier.ToGuid());
        published.Graph.SerialNumber.ShouldBe(AgvTestIdentifier);
        published.Graph.Nodes?.Length.ShouldBe(2);
        published.Graph.Edges?.Length.ShouldBe(1);
    }
}