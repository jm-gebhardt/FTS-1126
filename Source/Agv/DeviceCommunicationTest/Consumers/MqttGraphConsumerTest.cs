using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

/// <summary>
/// Tests for <see cref="MqttGraph"/> and <see cref="InternalGraphReceived"/>.
/// </summary>
[TestClass]
public class MqttGraphConsumerTest
{
    private const string AgvSerialNumber = "#0815#";

    /// <summary>
    /// Test for consuming AGV graph message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveGraphMessage_WithValidInput_ExpectMessage()
    {
        var fixture = new AgvGraphConsumerTestFixture();

        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1 }
        };
        var message = new MqttGraph(AgvSerialNumber, DateTimeOffset.UtcNow, nodes, edges, string.Empty);

        await fixture.TestHarness.Bus.PublishMqtt(message, null);

        // Did the test publish MqttGraph ...
        await fixture.ShouldHavePublishedAsync<MqttGraph>();
        // ... and has the message been consumed?
        var consumed = fixture.ConsumerHarness.Consumed.Select<MqttGraph>().First().Context.Message;
        consumed.SerialNumber.ShouldBe(AgvSerialNumber);

        // Did the consumer forward a Graph message ...
        await fixture.ShouldHavePublishedAsync<InternalGraphReceived>();
        // ... with expected content?
        var published = await fixture.GetFirstPublishedMessageAsync<InternalGraphReceived>();
        published.CorrelationId.ShouldBe(AgvSerialNumber.ToGuid());
        published.Graph.SerialNumber.ShouldBe(AgvSerialNumber);
        published.Graph.Nodes?.Length.ShouldBe(2);
        published.Graph.Edges?.Length.ShouldBe(1);
    }

    /// <summary>
    /// Test for consuming AGV graph message.
    /// </summary>
    [TestMethod]
    public async Task ReceiveGraphMessage_WithValidBezier5Input_ExpectMessage()
    {
        var fixture = new AgvGraphConsumerTestFixture();

        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new()
            {
                EdgeId = "1",
                StartId = "1",
                EndId = "2",
                Bidirectional = false,
                MaxVelocity = 1,
                EdgeType = "Bezier5",
                ControlPoints = new List<MqttControlPoint>
                {
                    new() { X = 1.0f, Y = 1.0f, Weight = 1.0f },
                    new() { X = 2.0f, Y = 2.0f, Weight = 1.0f },
                    new() { X = 3.0f, Y = 3.0f, Weight = 1.0f },
                    new() { X = 4.0f, Y = 4.0f, Weight = 1.0f }
                }
            }
        };

        var message = new MqttGraph(AgvSerialNumber, DateTimeOffset.UtcNow, nodes, edges, string.Empty);

        await fixture.TestHarness.Bus.PublishMqtt(message, null);

        // Did the test publish MqttGraph ...
        await fixture.ShouldHavePublishedAsync<MqttGraph>();
        // ... and has the message been consumed?
        var consumed = fixture.ConsumerHarness.Consumed.Select<MqttGraph>().First().Context.Message;
        consumed.SerialNumber.ShouldBe(AgvSerialNumber);

        // Did the consumer forward a Graph message ...
        await fixture.ShouldHavePublishedAsync<InternalGraphReceived>();
        // ... with expected content?
        var published = await fixture.GetFirstPublishedMessageAsync<InternalGraphReceived>();
        published.CorrelationId.ShouldBe(AgvSerialNumber.ToGuid());
        published.Graph.SerialNumber.ShouldBe(AgvSerialNumber);
        published.Graph.Nodes?.Length.ShouldBe(2);
        published.Graph.Edges?.Length.ShouldBe(1);
    }
}