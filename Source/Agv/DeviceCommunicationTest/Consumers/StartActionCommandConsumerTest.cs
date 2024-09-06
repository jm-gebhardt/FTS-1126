using System.Globalization;
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
            action.Target.NodeId.ShouldBe(toStationId);

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
            mqttMessage.Stations?.FromStation?.Id.ShouldBe(toStationId);
            mqttMessage.Stations?.FromStation?.Rotate.ShouldBeFalse();
            mqttMessage.Stations?.FromStation?.LiftHeight.ShouldBeNull();
            // ... and empty destination.
            mqttMessage.Stations?.ToStation.ShouldBeNull();
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
            action.Target.NodeId.ShouldBe(toStationId);

            // Get published MqttOrder Mqtt message
            IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

            // Check routing key and content type
            var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
            publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
            publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

            // Check content
            var mqttMessage = publishedMessage.Context.Message;

            mqttMessage.SerialNumber.ShouldBe(agvSerialNumber);

            mqttMessage.Stations?.FromStation.ShouldBeNull();
            mqttMessage.Stations?.ToStation?.Id.ShouldBe(toStationId);
            mqttMessage.Stations?.ToStation?.Rotate.ShouldBeFalse();
            mqttMessage.Stations?.ToStation?.LiftHeight.ShouldBeNull();
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
            action.Target.NodeId.ShouldBe(toStationId);

            // Get published MqttOrder Mqtt message
            IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

            // Check routing key and content type
            var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
            publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
            publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

            // Check content
            var mqttMessage = publishedMessage.Context.Message;

            mqttMessage.SerialNumber.ShouldBe(agvSerialNumber);
            mqttMessage.Stations?.FromStation.ShouldBeNull();
            mqttMessage.Stations?.ToStation?.Id.ShouldBe(toStationId);
            mqttMessage.Stations?.ToStation?.Rotate.ShouldBeFalse();
            mqttMessage.Stations?.ToStation?.LiftHeight.ShouldBeNull();
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
    public async Task Test_Send_PickAction_To_AGV_Via_MQTT_CorrectNodesAndEdges()
    {
        var fixture = new StartActionCommandConsumerTestFixture();

        // Create a publish observer
        var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
        {
            var actionCorrelationId = Guid.NewGuid();
            const string agvSerialNumber = "agv0";
            const string orderId = "1234";
            const string needsRotation = "2";
            var orderCorrelationId = orderId.ToGuid();
            const string toStationId = "Drop5";

            var action = new AgvAction(actionCorrelationId, ActionType.Pick, toStationId, orderCorrelationId)
            {
                Nodes = ["WP-001", "WP-002", "WP-003", toStationId],
                Edges = [Guid.Parse("ed70292e-e001-e002-8112-ffe69119c4b0"), Guid.Parse("ed70292e-e002-e003-8112-ffe69119c4b0"), Guid.NewGuid()],
                InfoToAgv =  new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.CargoRotation, needsRotation } }
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
            mqttMessage.Stations?.FromStation?.Id.ShouldBe(toStationId);
            mqttMessage.Stations?.FromStation?.Rotate.ShouldBeTrue();
            mqttMessage.Stations?.FromStation?.LiftHeight.ShouldBeNull();
            // ... and empty destination.
            mqttMessage.Stations?.ToStation.ShouldBeNull();
            mqttMessage.OrderType.ShouldBe(OrderType.Pick);
            mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);

            mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
            // Check nodes and edges, should not contain pick station and edge to pick station
            mqttMessage.Route?.Edges.Count.ShouldBe(2);
            mqttMessage.Route?.Nodes.Count.ShouldBe(3);
            List<object>? routeNodes = mqttMessage.Route?.Nodes;
            if (routeNodes != null)
            {
                routeNodes.Select(node => ((MqttNode)node).NodeId)
                    .ToList().ShouldBeEquivalentTo(action.Nodes.Take(routeNodes.Count).ToList());
            }
        }
    }
    
    /// <summary>
    /// Test consuming a StartActionCommand message (the drop action) and
    /// publish an MqttOrder Mqtt message with correct nodes and edges.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Test_Send_DropAction_To_AGV_Via_MQTT_CorrectNodesAndEdges()
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
            const double liftHeight = 1.23d;
            var action = new AgvAction(actionCorrelationId, ActionType.Drop, toStationId, orderCorrelationId, infoToAgv: new Dictionary<InfoToAgvKey, string>(){ {InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture)} })
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
            // For drop action Karis expects destination ToStation values ...
            mqttMessage.Stations?.ToStation?.Id.ShouldBe(toStationId);
            mqttMessage.Stations?.ToStation?.Rotate.ShouldBeFalse();
            mqttMessage.Stations?.ToStation?.LiftHeight.ShouldBe(liftHeight);
            // ... and empty fromStation.
            mqttMessage.Stations?.FromStation.ShouldBeNull();
            mqttMessage.OrderType.ShouldBe(OrderType.Drop);
            mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);

            mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
            // Check nodes and edges, should not contain drop station and edge to drop station
            mqttMessage.Route?.Edges.Count.ShouldBe(2);
            mqttMessage.Route?.Nodes.Count.ShouldBe(3);
            List<object>? routeNodes = mqttMessage.Route?.Nodes;
            if (routeNodes != null)
            {
                routeNodes.Select(node => ((MqttNode)node).NodeId)
                    .ToList().ShouldBeEquivalentTo(action.Nodes.Take(routeNodes.Count).ToList());
            }
        }
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the move action to a coordinate)
    /// and publish an MqttOrder Mqtt message with proper bus configuration.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task StartActionCommand_MoveToCoordinateAction_To_AGV_Via_MQTT()
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
            var target = new ActionTarget(12, 12, 0);

            var action = new AgvAction(actionCorrelationId, ActionType.Move, target, orderCorrelationId);
            var correlationId = Guid.NewGuid();
            var message = new StartActionCommand(correlationId, agvSerialNumber, action);

            // Publish StartActionCommand
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
            action.Target.ShouldBe(target);

            // Get published MqttOrder Mqtt message
            IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

            // Check routing key and content type
            var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
            publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
            publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

            // Check content
            var mqttMessage = publishedMessage.Context.Message;

            mqttMessage.SerialNumber.ShouldBe(agvSerialNumber);

            mqttMessage.Coordinates.ShouldBe(new Coordinates()
            {
                ToX = target.Position!.X,
                ToY = target.Position.Y,
                ToTheta = target.Position.Theta
            });
            mqttMessage.Stations.ShouldBeNull();
            mqttMessage.OrderType.ShouldBe(OrderType.Move);
            mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);

            mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
        }
    }

    [TestMethod]
    public async Task Test_StartAction_MoveToCoordinate_ActionTypeNotMove_ThrowsException()
    {
        var fixture = new StartActionCommandConsumerTestFixture();
        var action = new AgvAction(Guid.NewGuid(), ActionType.Pick, new ActionTarget(12, 12, 0), Guid.NewGuid());
        await fixture.TestHarness.Bus.Publish(new StartActionCommand(Guid.NewGuid(), "agv-0", action));
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        await fixture.ShouldHavePublishedAsync<StartActionCommand>();

        IPublishedMessage<Fault<StartActionCommand>>? fault = fixture.TestHarness.Published.Select<Fault<StartActionCommand>>().First();
        fault.Context.Message.Exceptions.ShouldContain(e => e.ExceptionType == typeof(InvalidOperationException).FullName);
    }
}