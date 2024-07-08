using System.Reflection;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using MassTransit;
using MassTransit.Serialization;
using MassTransit.Testing;
using MassTransit.Testing.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

/// <summary>
/// Test class for consuming an ExecuteOrder message and publish an MqttOrder Mqtt message with proper bus configuration
/// </summary>
[TestClass]
public class StartActionCommandConsumerTest
{
    /// <summary>
    /// Test consuming an ExecuteOrder message and publish an MqttOrder Mqtt message with proper bus configuration
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Test_Send_TransportOrder_To_AGV_Via_MQTT()
    {
        var fixture = new StartActionCommandConsumerTestFixture();

        // Create a publish observer
        var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
        {
            var actionCorrelationId = Guid.NewGuid();
            const string agvSerialNumber = "agv0";
            const string orderId = "1234";
            var orderCorrelationId = orderId.ToGuid();
            const string toStationId = "Drop5";

            var action = new AgvAction(actionCorrelationId, ActionType.Pick, toStationId, orderCorrelationId);
            var correlationId = Guid.NewGuid();
            var message = new StartActionCommand(correlationId, agvSerialNumber, action);

            // Publish ExecuteTransportOrder
            await fixture.TestHarness.Bus.Publish(message);

            // Get Consumer Harness
            IConsumerTestHarness<StartActionCommandConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<StartActionCommandConsumer>();

            // Check if consumed
            (await consumerHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();
            // Get consumed message
            IReceivedMessage<StartActionCommand> consumedMessage = consumerHarness.Consumed.Select<StartActionCommand>().First();
            action = consumedMessage.Context.Message.Action;

            // Check consumed message
            action.CorrelationId.ShouldBe(actionCorrelationId);
            action.ParentId.ShouldBe(orderCorrelationId);
            action.TargetNodeId.ShouldBe(toStationId);

            // Get published MqttOrder Mqtt message
            IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

            // Check routing key and content type
            var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
            publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
            publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

            // Check content
            var mqttMessage = publishedMessage.Context.Message;

            mqttMessage.SerialNumber.ShouldBe(agvSerialNumber);
            // For pick action Karis expects destination as source ...
            mqttMessage.Stations?.FromStationIdentifier.ShouldBe(toStationId);
            // ... and empty destination.
            mqttMessage.Stations?.ToStationIdentifier.ShouldBe(string.Empty);
            mqttMessage.OrderType.ShouldBe(OrderType.Pick);
            mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);

            mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
        }
    }

    [TestMethod]
    public async Task Test_StartAction_EmptyAssignedAgv_ThrowsException()
    {
        var fixture = new StartActionCommandConsumerTestFixture();
        var action = new AgvAction(Guid.Empty, ActionType.Pick, string.Empty, Guid.Empty);
        await fixture.TestHarness.Bus.Publish(new StartActionCommand(Guid.Empty, "", action));
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldHavePublishedAsync<StartActionCommand>();

        IPublishedMessage<Fault<StartActionCommand>>? fault = fixture.TestHarness.Published.Select<Fault<StartActionCommand>>().First();
        fault.Context.Message.Exceptions.ShouldContain(e => e.ExceptionType == typeof(AssignedAgvNotFoundException).FullName);
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the move action of a charge action group)
    /// and publish an MqttOrder Mqtt message with proper bus configuration.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task StartActionCommand_MoveAction_To_AGV_Via_MQTT()
    {
        var fixture = new StartActionCommandConsumerTestFixture();

        // Create a publish observer
        var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
        {
            var actionCorrelationId = Guid.NewGuid();
            const string agvSerialNumber = "agv0";
            const string orderId = "1234";
            var orderCorrelationId = orderId.ToGuid();
            const string toStationId = "Charge-1";

            var action = new AgvAction(actionCorrelationId, ActionType.Move, toStationId, orderCorrelationId);
            var correlationId = Guid.NewGuid();
            var message = new StartActionCommand(correlationId, agvSerialNumber, action);

            // Publish ExecuteTransportOrder
            await fixture.TestHarness.Bus.Publish(message);

            // Get Consumer Harness
            IConsumerTestHarness<StartActionCommandConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<StartActionCommandConsumer>();

            // Check if consumed
            (await consumerHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();
            // Get consumed message
            IReceivedMessage<StartActionCommand> consumedMessage = consumerHarness.Consumed.Select<StartActionCommand>().First();
            action = consumedMessage.Context.Message.Action;

            // Check consumed message
            action.CorrelationId.ShouldBe(actionCorrelationId);
            action.ParentId.ShouldBe(orderCorrelationId);
            action.TargetNodeId.ShouldBe(toStationId);

            // Get published MqttOrder Mqtt message
            IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

            // Check routing key and content type
            var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
            publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
            publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

            // Check content
            var mqttMessage = publishedMessage.Context.Message;

            mqttMessage.SerialNumber.ShouldBe(agvSerialNumber);

            mqttMessage.Stations?.FromStationIdentifier.ShouldBe(string.Empty);
            mqttMessage.Stations?.ToStationIdentifier.ShouldBe(toStationId);
            mqttMessage.OrderType.ShouldBe(OrderType.Move);
            mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);

            mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
        }
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the charge action of a charge action group) and
    /// publish an MqttOrder Mqtt message with proper bus configuration.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task StartActionCommand_ChargeAction_To_AGV_Via_MQTT()
    {
        var fixture = new StartActionCommandConsumerTestFixture();

        // Create a publish observer
        var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
        {
            var actionCorrelationId = Guid.NewGuid();
            const string agvSerialNumber = "agv0";
            const string orderId = "1234";
            var orderCorrelationId = orderId.ToGuid();
            const string toStationId = "Charge-1";

            var action = new AgvAction(actionCorrelationId, ActionType.Charge, toStationId, orderCorrelationId);
            var correlationId = Guid.NewGuid();
            var message = new StartActionCommand(correlationId, agvSerialNumber, action);

            // Publish ExecuteTransportOrder
            await fixture.TestHarness.Bus.Publish(message);

            // Get Consumer Harness
            IConsumerTestHarness<StartActionCommandConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<StartActionCommandConsumer>();

            // Check if consumed
            (await consumerHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();
            // Get consumed message
            IReceivedMessage<StartActionCommand> consumedMessage = consumerHarness.Consumed.Select<StartActionCommand>().First();
            action = consumedMessage.Context.Message.Action;

            // Check consumed message
            action.CorrelationId.ShouldBe(actionCorrelationId);
            action.ParentId.ShouldBe(orderCorrelationId);
            action.TargetNodeId.ShouldBe(toStationId);

            // Get published MqttOrder Mqtt message
            IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

            // Check routing key and content type
            var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
            publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
            publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

            // Check content
            var mqttMessage = publishedMessage.Context.Message;

            mqttMessage.SerialNumber.ShouldBe(agvSerialNumber);
            mqttMessage.Stations?.FromStationIdentifier.ShouldBe(string.Empty);
            mqttMessage.Stations?.ToStationIdentifier.ShouldBe(toStationId);
            mqttMessage.OrderType.ShouldBe(OrderType.Charge);
            mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);

            mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
        }
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the pick action) and
    /// publish an MqttOrder Mqtt message with correct nodes and edges.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Test_Send_TransportOrder_To_AGV_Via_MQTT_CorrectNodesAndEdges()
    {
        var fixture = new StartActionCommandConsumerTestFixture();

        // Create a publish observer
        var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
        {
            var actionCorrelationId = Guid.NewGuid();
            const string agvSerialNumber = "agv0";
            const string orderId = "1234";
            var orderCorrelationId = orderId.ToGuid();
            const string toStationId = "Drop5";

            var action = new AgvAction(actionCorrelationId, ActionType.Pick, toStationId, orderCorrelationId)
            {
                Nodes = ["WP-001", "WP-002", "WP-003", toStationId],
                Edges = [Guid.Parse("ed70292e-e001-e002-8112-ffe69119c4b0"), Guid.Parse("ed70292e-e002-e003-8112-ffe69119c4b0"), Guid.NewGuid()]
            };
            var correlationId = Guid.NewGuid();
            var message = new StartActionCommand(correlationId, agvSerialNumber, action);

            // Publish ExecuteTransportOrder
            await fixture.TestHarness.Bus.Publish(message);

            // Get Consumer Harness
            IConsumerTestHarness<StartActionCommandConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<StartActionCommandConsumer>();

            // Check if consumed
            (await consumerHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();
            // Get consumed message
            IReceivedMessage<StartActionCommand> consumedMessage = consumerHarness.Consumed.Select<StartActionCommand>().First();
            action = consumedMessage.Context.Message.Action;

            // Get published MqttOrder Mqtt message
            IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

            // Check content
            var mqttMessage = publishedMessage.Context.Message;

            mqttMessage.SerialNumber.ShouldBe(agvSerialNumber);
            // For pick action Karis expects destination as source ...
            mqttMessage.Stations?.FromStationIdentifier.ShouldBe(toStationId);
            // ... and empty destination.
            mqttMessage.Stations?.ToStationIdentifier.ShouldBe(string.Empty);
            mqttMessage.OrderType.ShouldBe(OrderType.Pick);
            mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);

            mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
            // Check nodes and edges, should not contain pick station and edge to pick station
            mqttMessage.Route?.Edges.Count.ShouldBe(2);
            mqttMessage.Route?.Nodes.Count.ShouldBe(3);
            List<object>? routeNodes = mqttMessage.Route?.Nodes;
            if (routeNodes != null)
            {
                for (int i = 0; i < routeNodes.Count; i++)
                {
                    var routeNode = (MqttNode)routeNodes[i];
                    routeNode.NodeId.ShouldBe(action.Nodes[i]);
                }
            }
        }
    }
}