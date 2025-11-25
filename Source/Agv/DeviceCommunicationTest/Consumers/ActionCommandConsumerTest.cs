using System.Globalization;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using MassTransit;
using MassTransit.Testing;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

[TestClass]
public class ActionCommandConsumerTest
{
    private const string AgvSerialNumber = "agv0";
    private readonly Guid _agvCorrelationId = AgvSerialNumber.ToGuid();
    private const string ToStation = "Target";
    private static readonly GraphNode _toStationNode = ToStation.ToGraphNode();
    private const string OrderId = "1234";
    private readonly Guid _orderCorrelationId = OrderId.ToGuid();

    /// <summary>
    /// Test consuming an ExecuteOrder message and publish an MqttOrder Mqtt message with proper bus configuration
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Test_Send_TransportOrder_To_AGV_Via_MQTT()
    {
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Pick,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.OrderType.ShouldBe(OrderType.Pick);
        mqtt.RecordType.ShouldBe(OrderRecordType.Order);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.Stations.FromStation.DockFront.ShouldBeFalse();
        mqtt.Stations.FromStation.LiftHeight.ShouldBeNull();
        mqtt.Stations.ToStation.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_EmptyAssignedAgv_ThrowsException()
    {
        var action = new AgvAction(Guid.Empty, ActionType.Pick, targetNode: null, Guid.Empty);
        var fixture = new ActionCommandConsumerTestFixture();

        await fixture.TestHarness.Bus.Publish(new StartActionCommand(Guid.Empty, "", action));
        await fixture.ShouldHaveConsumedAsync<StartActionCommand>();
        (await fixture.TestHarness.Published.Any<Fault<StartActionCommand>>()).ShouldBeTrue();

        IPublishedMessage<Fault<StartActionCommand>>? fault = fixture.TestHarness.Published.Select<Fault<StartActionCommand>>().First();
        fault.Context.Message.Exceptions.ShouldContain(e => e.ExceptionType == typeof(AssignedAgvNotFoundException).FullName);
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the move action of a charge action group)
    /// and publish an MqttOrder Mqtt message with proper bus configuration.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Consume_StartActionCommand_MoveAction_To_AGV_Via_MQTT()
    {
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Move,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldBeNull();
        mqtt.Stations.ToStation.ShouldNotBeNull();
        mqtt.Stations.ToStation.Id.ShouldBe(ToStation);
        mqtt.Stations.ToStation.DockFront.ShouldBeFalse();
        mqtt.Stations.ToStation.LiftHeight.ShouldBeNull();
        mqtt.OrderType.ShouldBe(OrderType.Move);
        mqtt.RecordType.ShouldBe(OrderRecordType.Order);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the charge action of a charge action group) and
    /// publish an MqttOrder Mqtt message with proper bus configuration.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Consume_StartActionCommand_ChargeAction_To_AGV_Via_MQTT()
    {
        var action = new AgvAction(Guid.NewGuid(), ActionType.Charge, _toStationNode, _orderCorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string>
            {
                { InfoToAgvKey.DockFront, "true" }
            });
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldBeNull();
        mqtt.Stations.ToStation.ShouldNotBeNull();
        mqtt.Stations.ToStation.Id.ShouldBe(ToStation);
        mqtt.Stations.ToStation.DockFront.ShouldBeTrue();
        mqtt.Stations.ToStation.LiftHeight.ShouldBeNull();
        mqtt.OrderType.ShouldBe(OrderType.Charge);
        mqtt.RecordType.ShouldBe(OrderRecordType.Order);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the pick action) and
    /// publish an MqttOrder Mqtt message with correct nodes and edges.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Test_Send_PickAction_To_AGV_Via_MQTT_CorrectNodesAndEdges()
    {
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        Guid[] edges = [Guid.Parse("ed70292e-e001-e002-8112-ffe69119c4b0"), Guid.Parse("ed70292e-e002-e003-8112-ffe69119c4b0"), Guid.NewGuid()];
        var action = new AgvAction(Guid.NewGuid(), ActionType.Pick, _toStationNode, _orderCorrelationId)
        {
            Nodes = nodes.ToGraphNodeArray(),
            Edges = edges,
            InfoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, "true" } }
        };
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);

        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.Stations.FromStation.DockFront.ShouldBeTrue();
        mqtt.Stations.ToStation.ShouldBeNull();
        mqtt.Route?.Edges.Count.ShouldBe(2);
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the drop action) and
    /// publish an MqttOrder Mqtt message with correct nodes and edges.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Test_Send_DropAction_To_AGV_Via_MQTT_CorrectNodesAndEdges()
    {
        const double liftHeight = 1.23d;
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = new AgvAction(Guid.NewGuid(), ActionType.Drop, _toStationNode, _orderCorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } })
        {
            Nodes = nodes.ToGraphNodeArray()
        };
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);

        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldBeNull();
        mqtt.Stations.ToStation.ShouldNotBeNull();
        mqtt.Stations.ToStation.Id.ShouldBe(ToStation);
        mqtt.Stations.ToStation.DockFront.ShouldBeTrue();
        mqtt.Stations.ToStation.LiftHeight.ShouldBe(liftHeight);
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    /// <summary>
    /// Test consuming a StartActionCommand message (the move action to a coordinate)
    /// and publish an MqttOrder Mqtt message with proper bus configuration.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Consume_StartActionCommand_MoveToCoordinateAction_To_AGV_Via_MQTT()
    {
        var target = new ActionTarget(12, 12, 0);
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, target, _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.Coordinates.ShouldBe(new Coordinates
        {
            ToX = target.Position!.X,
            ToY = target.Position.Y * -1,
            ToTheta = (float) Mathematics.ConvertToRadians(90.0f) - target.Position.Theta
        });
        mqtt.Stations.ShouldBeNull();
        mqtt.OrderType.ShouldBe(OrderType.Move);
        mqtt.RecordType.ShouldBe(OrderRecordType.Order);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_MoveToCoordinate_MqttMessageSent()
    {
        var target = new ActionTarget(12, 12, 0);
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, target, _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new UpdateActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.Coordinates.ShouldBe(new Coordinates
        {
            ToX = target.Position!.X,
            ToY = target.Position.Y * -1,
            ToTheta = (float) Mathematics.ConvertToRadians(90.0f) - target.Position.Theta
        });
        mqtt.Stations.ShouldBeNull();
        mqtt.OrderType.ShouldBe(OrderType.Move);
        mqtt.RecordType.ShouldBe(OrderRecordType.OrderUpdate);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_PreviouslyAbortedAndStartedOrder_MqttOrderWithPreviousIdSent()
    {
        var vehicleId = Guid.NewGuid();

        const double liftHeight = 1.23d;
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = new AgvAction(Guid.NewGuid(), ActionType.Drop, _toStationNode, _orderCorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } })
        {
            Nodes = nodes.ToGraphNodeArray()
        };
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(vehicleId, action.CorrelationId);
        await fixture.FillGraphRepository();

        var command = new UpdateActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, MqttOrder>(command);

        mqtt.OrderId.ShouldBe(vehicleId.ToString());
        mqtt.Stations?.ToStation?.Id.ShouldBe(ToStation);
        mqtt.Stations?.ToStation?.DockFront.ShouldBeTrue();
        mqtt.Stations?.ToStation?.LiftHeight.ShouldBe(liftHeight);
        mqtt.Stations?.FromStation.ShouldBeNull();
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_Drop_MqttMessageSent()
    {
        const double liftHeight = 1.23d;
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = new AgvAction(Guid.NewGuid(), ActionType.Drop, _toStationNode, _orderCorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } })
        {
            Nodes = nodes.ToGraphNodeArray()
        };
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var command = new UpdateActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, MqttOrder>(command);

        mqtt.OrderType.ShouldBe(OrderType.Drop);
        mqtt.RecordType.ShouldBe(OrderRecordType.OrderUpdate);
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.ToStation.ShouldNotBeNull();
        mqtt.Stations.ToStation.Id.ShouldBe(ToStation);
        mqtt.Stations.ToStation.DockFront.ShouldBeTrue();
        mqtt.Stations.ToStation.LiftHeight.ShouldBe(liftHeight);
        mqtt.Stations.FromStation.ShouldBeNull();
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_Pick_MqttMessageSent()
    {
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = new AgvAction(Guid.NewGuid(), ActionType.Pick, _toStationNode, _orderCorrelationId)
        {
            Nodes = nodes.ToGraphNodeArray(),
            InfoToAgv = new Dictionary<InfoToAgvKey, string>
            {
                {
                    InfoToAgvKey.DockFront, "true"
                }
            }
        };
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var command = new UpdateActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, MqttOrder>(command);

        mqtt.OrderType.ShouldBe(OrderType.Pick);
        mqtt.RecordType.ShouldBe(OrderRecordType.OrderUpdate);
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.Stations.FromStation.DockFront.ShouldBeTrue();
        mqtt.Stations.FromStation.LiftHeight.ShouldBeNull();
        mqtt.Stations.ToStation.ShouldBeNull();
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_Charge_MqttMessageSent()
    {
        var action = new AgvAction(Guid.NewGuid(), ActionType.Charge, _toStationNode, _orderCorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, "true" } });
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new UpdateActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.ToStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldBeNull();
        mqtt.Stations.ToStation.Id.ShouldBe(ToStation);
        mqtt.Stations.ToStation.DockFront.ShouldBeTrue();
        mqtt.Stations.ToStation.LiftHeight.ShouldBeNull();
        mqtt.OrderType.ShouldBe(OrderType.Charge);
        mqtt.RecordType.ShouldBe(OrderRecordType.OrderUpdate);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_MoveToNode_MqttMessageSent()
    {
        var action = AgvActionGenerator.GenerateAction(actionType: ActionType.Move, targetNodeId: ToStation, parentId: _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new UpdateActionCommand(_agvCorrelationId, AgvSerialNumber, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldBeNull();
        mqtt.Stations.ToStation.ShouldNotBeNull();
        mqtt.Stations.ToStation.Id.ShouldBe(ToStation);
        mqtt.Stations.ToStation.DockFront.ShouldBeFalse();
        mqtt.Stations.ToStation.LiftHeight.ShouldBeNull();
        mqtt.OrderType.ShouldBe(OrderType.Move);
        mqtt.RecordType.ShouldBe(OrderRecordType.OrderUpdate);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_UpdateActionCommand_EmptyAssignedAgv_ThrowsException()
    {
        var action = new AgvAction(Guid.Empty, ActionType.Pick, targetNode: "".ToGraphNode(), Guid.Empty);
        var fixture = new ActionCommandConsumerTestFixture();

        await fixture.TestHarness.Bus.Publish(new UpdateActionCommand(Guid.Empty, "", action));
        await fixture.ShouldHaveConsumedAsync<UpdateActionCommand>();
        (await fixture.TestHarness.Published.Any<Fault<UpdateActionCommand>>()).ShouldBeTrue();

        IPublishedMessage<Fault<UpdateActionCommand>>? fault = fixture.TestHarness.Published.Select<Fault<UpdateActionCommand>>().First();
        fault.Context.Message.Exceptions.ShouldContain(e =>
            e.ExceptionType == typeof(AssignedAgvNotFoundException).FullName);
    }

    [TestMethod]
    public async Task Consume_AbortAndStartActionCommand_NoAbortedAction_MqttMessageSentWithRecordTypeOrder()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = new AgvAction(Guid.NewGuid(), ActionType.Pick, _toStationNode, _orderCorrelationId)
        {
            Nodes = nodes.ToGraphNodeArray(),
            InfoToAgv = new Dictionary<InfoToAgvKey, string>
            {
                {
                    InfoToAgvKey.DockFront, "true"
                }
            }
        };

        var cmd = new AbortAndStartActionCommand(_agvCorrelationId, AgvSerialNumber, action, null);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<AbortAndStartActionCommand, MqttOrder>(cmd);

        mqtt.OrderType.ShouldBe(OrderType.Pick);
        mqtt.RecordType.ShouldBe(OrderRecordType.Order);
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.Stations.FromStation.DockFront.ShouldBeTrue();
        mqtt.Stations.ToStation.ShouldBeNull();
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    [TestMethod]
    public async Task Consume_AbortAndStartActionCommand_AbortedAction_MqttMessageSentWithRecordTypeOrderUpdate()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var abortedId = Guid.NewGuid();
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = new AgvAction(Guid.NewGuid(), ActionType.Pick, _toStationNode, _orderCorrelationId)
        {
            Nodes = nodes.ToGraphNodeArray(),
            InfoToAgv = new Dictionary<InfoToAgvKey, string>
            {
                {
                    InfoToAgvKey.DockFront, "true"
                }
            }
        };

        var cmd = new AbortAndStartActionCommand(_agvCorrelationId, AgvSerialNumber, action, abortedId);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<AbortAndStartActionCommand, MqttOrder>(cmd);

        mqtt.OrderType.ShouldBe(OrderType.Pick);
        mqtt.RecordType.ShouldBe(OrderRecordType.OrderUpdate);
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.Stations.FromStation.DockFront.ShouldBeTrue();
        // ... and empty destination.
        mqtt.Stations.ToStation.ShouldBeNull();
        mqtt.OrderId.ShouldBe(abortedId.ToString());
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    [TestMethod]
    public async Task Consume_ResendAction_ValidData_PublishesMqttOrder()
    {
        var action = AgvActionGenerator.GenerateAction(actionType: ActionType.Pick, targetNodeId: ToStation);
        var fixture = new ActionCommandConsumerTestFixture();
        var resend = new ResendAction(_agvCorrelationId, AgvSerialNumber, action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<ResendAction, MqttOrder>(resend);

        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
    }

    [TestMethod]
    public async Task Consume_ResendAction_UpdatedOrder_PublishesMqttOrder()
    {
        var vehicleOrderId = Guid.NewGuid();
        var action = AgvActionGenerator.GenerateAction(actionType: ActionType.Pick, targetNodeId: ToStation);
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(vehicleOrderId, action.CorrelationId);

        var resend = new ResendAction(_agvCorrelationId, AgvSerialNumber, action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<ResendAction, MqttOrder>(resend);

        mqtt.OrderId.ShouldBe(vehicleOrderId.ToString());
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_PickDrop_HasRightOrderIds()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions.First();
        var dropAction = actionGroup.Actions.Last();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, pickAction);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);
        mqtt.OrderId.ShouldBe(pickAction.CorrelationId.ToString());

        command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, dropAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command, 2);
        mqtt.OrderId.ShouldBe(dropAction.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_MovePickDrop_HasRightOrderIds()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        var actionGroup = AgvActionGenerator.GenerateMovePickDropActionGroup();
        var moveAction = actionGroup.Actions.First();
        var pickAction = actionGroup.Actions.Skip(1).First();
        var dropAction = actionGroup.Actions.Skip(2).First();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, moveAction);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);
        mqtt.OrderId.ShouldBe(moveAction.CorrelationId.ToString());

        command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, pickAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command, 2);
        mqtt.OrderId.ShouldBe(pickAction.CorrelationId.ToString());

        command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, dropAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command, 3);
        mqtt.OrderId.ShouldBe(dropAction.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_StartActionCommand_PickMoveDrop_HasRightOrderIds()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickMoveDropActionGroup();
        var pickAction = actionGroup.Actions.First();
        var moveAction = actionGroup.Actions.Skip(1).First();
        var dropAction = actionGroup.Actions.Skip(2).First();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, pickAction);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);
        mqtt.OrderId.ShouldBe(pickAction.CorrelationId.ToString());

        command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, moveAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command, 2);
        mqtt.OrderId.ShouldBe(moveAction.CorrelationId.ToString());

        command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, dropAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command, 3);
        mqtt.OrderId.ShouldBe(dropAction.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_StartActionCommandAndUpdateActionCommand_PickAndMoveGetsUpdatedThenDrop_HasRightOrderIds()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickMoveDropActionGroup();
        var pickAction = actionGroup.Actions.First();
        var moveAction = actionGroup.Actions.Skip(1).First();
        var dropAction = actionGroup.Actions.Skip(2).First();

        var command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, pickAction);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command);
        mqtt.OrderId.ShouldBe(pickAction.CorrelationId.ToString());
        await fixture.TryRemoveMappingAsync(mqtt.OrderId.ToGuid(), pickAction.CorrelationId);

        command = new StartActionCommand(_agvCorrelationId, AgvSerialNumber, moveAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, MqttOrder>(command, 2);
        mqtt.OrderId.ShouldBe(moveAction.CorrelationId.ToString());

        var updateCommand = new UpdateActionCommand(_agvCorrelationId, AgvSerialNumber, dropAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, MqttOrder>(updateCommand, 3);
        mqtt.OrderId.ShouldBe(dropAction.CorrelationId.ToString());
    }
}