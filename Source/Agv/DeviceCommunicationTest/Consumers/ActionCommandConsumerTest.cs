using System.Globalization;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
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
    private const string OrderId = "1234";
    private readonly Guid _orderCorrelationId = OrderId.ToGuid();

    /// <summary>
    /// Test consuming a DispatchRouteCommand and publish an MqttOrder with proper bus configuration.
    /// </summary>
    [TestMethod]
    public async Task Test_Send_TransportOrder_To_AGV_Via_MQTT()
    {
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Pick,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);

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
    public async Task Consume_DispatchRouteCommand_EmptyAssignedAgv_ThrowsException()
    {
        var action = AgvActionGenerator.GenerateAction(correlationId: Guid.Empty, actionType: ActionType.Pick, targetNodeId: "", parentId: Guid.Empty);
        var fixture = new ActionCommandConsumerTestFixture();

        await fixture.TestHarness.Bus.Publish(new DispatchRouteCommand(Guid.Empty, "", new RoutePlan([], [], [], []), action));
        await fixture.ShouldHaveConsumedAsync<DispatchRouteCommand>();
        (await fixture.TestHarness.Published.Any<Fault<DispatchRouteCommand>>()).ShouldBeTrue();

        IPublishedMessage<Fault<DispatchRouteCommand>>? fault = fixture.TestHarness.Published.Select<Fault<DispatchRouteCommand>>().First();
        fault.Context.Message.Exceptions.ShouldContain(e => e.ExceptionType == typeof(AssignedAgvNotFoundException).FullName);
    }

    /// <summary>
    /// Test consuming a DispatchRouteCommand (the move action of a charge action group)
    /// and publish an MqttOrder with proper bus configuration.
    /// </summary>
    [TestMethod]
    public async Task Consume_DispatchRouteCommand_MoveAction_To_AGV_Via_MQTT()
    {
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Move,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);

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
    /// Test consuming a DispatchRouteCommand (the charge action of a charge action group) and
    /// publish an MqttOrder with proper bus configuration.
    /// </summary>
    [TestMethod]
    public async Task Consume_DispatchRouteCommand_ChargeAction_To_AGV_Via_MQTT()
    {
        var action = AgvActionGenerator.GenerateChargeAction(
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string>
            {
                { InfoToAgvKey.DockFront, "true" }
            });
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);

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
    /// Test consuming a DispatchRouteCommand (the pick action) and publish an MqttOrder with correct nodes and edges.
    /// </summary>
    [TestMethod]
    public async Task Test_Send_PickAction_To_AGV_Via_MQTT_CorrectNodesAndEdges()
    {
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        Guid[] edges = [Guid.Parse("ed70292e-e001-e002-8112-ffe69119c4b0"), Guid.Parse("ed70292e-e002-e003-8112-ffe69119c4b0"), Guid.NewGuid()];
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Pick,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            nodes: nodes.ToGraphNodeArray(),
            edges: edges,
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, "true" } });
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var routePlan = RoutePlanGenerator.Build(nodes.SkipLast(1).ToArray().ToGraphNodeArray(), edges.SkipLast(1).ToArray());
        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, routePlan, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);

        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.Stations.FromStation.DockFront.ShouldBeTrue();
        mqtt.Stations.ToStation.ShouldBeNull();
        mqtt.Route?.Edges.Count.ShouldBe(2);
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    /// <summary>
    /// Test consuming a DispatchRouteCommand (the drop action) and publish an MqttOrder with correct nodes and edges.
    /// </summary>
    [TestMethod]
    public async Task Test_Send_DropAction_To_AGV_Via_MQTT_CorrectNodesAndEdges()
    {
        const double liftHeight = 1.23d;
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Drop,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            nodes: nodes.ToGraphNodeArray(),
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } });
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var routePlan = new RoutePlan(nodes.SkipLast(1).ToArray().ToGraphNodeArray(), [], [], []);
        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, routePlan, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);

        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldBeNull();
        mqtt.Stations.ToStation.ShouldNotBeNull();
        mqtt.Stations.ToStation.Id.ShouldBe(ToStation);
        mqtt.Stations.ToStation.DockFront.ShouldBeTrue();
        mqtt.Stations.ToStation.LiftHeight.ShouldBe(liftHeight);
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    /// <summary>
    /// Test consuming a DispatchRouteCommand (the move action to a coordinate)
    /// and publish an MqttOrder with proper bus configuration.
    /// </summary>
    [TestMethod]
    public async Task Consume_DispatchRouteCommand_MoveToCoordinateAction_To_AGV_Via_MQTT()
    {
        var target = new ActionTarget(12, 12, 0);
        var action = AgvActionGenerator.GenerateMoveToCoordinateAction(target, _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.Coordinates.ShouldBe(new Coordinates
        {
            ToX = target.Position!.X,
            ToY = target.Position.Y,
            ToTheta = target.Position.Theta
        });
        mqtt.Stations.ShouldBeNull();
        mqtt.OrderType.ShouldBe(OrderType.Move);
        mqtt.RecordType.ShouldBe(OrderRecordType.Order);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_UpdateDispatchedRouteCommand_MoveToCoordinate_MqttMessageSent()
    {
        var target = new ActionTarget(12, 12, 0);
        var action = AgvActionGenerator.GenerateMoveToCoordinateAction(target, _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new UpdateDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateDispatchedRouteCommand, MqttOrder>(command);

        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
        mqtt.Coordinates.ShouldBe(new Coordinates
        {
            ToX = target.Position!.X,
            ToY = target.Position.Y,
            ToTheta = target.Position.Theta
        });
        mqtt.Stations.ShouldBeNull();
        mqtt.OrderType.ShouldBe(OrderType.Move);
        mqtt.RecordType.ShouldBe(OrderRecordType.OrderUpdate);
        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_UpdateDispatchedRouteCommand_PreviouslyAbortedAndStartedOrder_MqttOrderWithPreviousIdSent()
    {
        var vehicleId = Guid.NewGuid();

        const double liftHeight = 1.23d;
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Drop,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            nodes: nodes.ToGraphNodeArray(),
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } });
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(vehicleId, action.CorrelationId);
        await fixture.FillGraphRepository();

        var routePlan = new RoutePlan(nodes.SkipLast(1).ToArray().ToGraphNodeArray(), [], [], []);
        var command = new UpdateDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, routePlan, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateDispatchedRouteCommand, MqttOrder>(command);

        mqtt.OrderId.ShouldBe(vehicleId.ToString());
        mqtt.Stations?.ToStation?.Id.ShouldBe(ToStation);
        mqtt.Stations?.ToStation?.DockFront.ShouldBeTrue();
        mqtt.Stations?.ToStation?.LiftHeight.ShouldBe(liftHeight);
        mqtt.Stations?.FromStation.ShouldBeNull();
        mqtt.Route?.Nodes.Count.ShouldBe(3);
    }

    [TestMethod]
    public async Task Consume_UpdateDispatchedRouteCommand_Drop_MqttMessageSent()
    {
        const double liftHeight = 1.23d;
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Drop,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            nodes: nodes.ToGraphNodeArray(),
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } });
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var routePlan = new RoutePlan(nodes.SkipLast(1).ToArray().ToGraphNodeArray(), [], [], []);
        var command = new UpdateDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, routePlan, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateDispatchedRouteCommand, MqttOrder>(command);

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
    public async Task Consume_UpdateDispatchedRouteCommand_Pick_MqttMessageSent()
    {
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Pick,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            nodes: nodes.ToGraphNodeArray(),
            infoToAgv: new Dictionary<InfoToAgvKey, string>
            {
                { InfoToAgvKey.DockFront, "true" }
            });
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var routePlan = new RoutePlan(nodes.SkipLast(1).ToArray().ToGraphNodeArray(), [], [], []);
        var command = new UpdateDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, routePlan, action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateDispatchedRouteCommand, MqttOrder>(command);

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
    public async Task Consume_UpdateDispatchedRouteCommand_Charge_MqttMessageSent()
    {
        var action = AgvActionGenerator.GenerateChargeAction(
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            infoToAgv: new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, "true" } });
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new UpdateDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateDispatchedRouteCommand, MqttOrder>(command);

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
    public async Task Consume_UpdateDispatchedRouteCommand_MoveToNode_MqttMessageSent()
    {
        var action = AgvActionGenerator.GenerateAction(actionType: ActionType.Move, targetNodeId: ToStation, parentId: _orderCorrelationId);
        var fixture = new ActionCommandConsumerTestFixture();

        var command = new UpdateDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<UpdateDispatchedRouteCommand, MqttOrder>(command);

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
    public async Task Consume_UpdateDispatchedRouteCommand_EmptyAssignedAgv_ThrowsException()
    {
        var action = AgvActionGenerator.GenerateAction(correlationId: Guid.Empty, actionType: ActionType.Pick, targetNodeId: "", parentId: Guid.Empty);
        var fixture = new ActionCommandConsumerTestFixture();

        await fixture.TestHarness.Bus.Publish(new UpdateDispatchedRouteCommand(Guid.Empty, "", new RoutePlan([], [], [], []), action));
        await fixture.ShouldHaveConsumedAsync<UpdateDispatchedRouteCommand>();
        (await fixture.TestHarness.Published.Any<Fault<UpdateDispatchedRouteCommand>>()).ShouldBeTrue();

        IPublishedMessage<Fault<UpdateDispatchedRouteCommand>>? fault = fixture.TestHarness.Published.Select<Fault<UpdateDispatchedRouteCommand>>().First();
        fault.Context.Message.Exceptions.ShouldContain(e =>
            e.ExceptionType == typeof(AssignedAgvNotFoundException).FullName);
    }

    [TestMethod]
    public async Task Consume_AbortAndDispatchRouteCommand_NoAbortedAction_MqttMessageSentWithRecordTypeOrder()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Pick,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            nodes: nodes.ToGraphNodeArray(),
            infoToAgv: new Dictionary<InfoToAgvKey, string>
            {
                { InfoToAgvKey.DockFront, "true" }
            });

        var routePlan = new RoutePlan(nodes.SkipLast(1).ToArray().ToGraphNodeArray(), [], [], []);
        var cmd = new AbortAndDispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, action, routePlan, null);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<AbortAndDispatchRouteCommand, MqttOrder>(cmd);

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
    public async Task Consume_AbortAndDispatchRouteCommand_AbortedAction_MqttMessageSentWithRecordTypeOrderUpdate()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.FillGraphRepository();

        var abortedId = Guid.NewGuid();
        string[] nodes = ["WP-001", "WP-002", "WP-003", ToStation];
        var action = AgvActionGenerator.GenerateAction(
            actionType: ActionType.Pick,
            targetNodeId: ToStation,
            parentId: _orderCorrelationId,
            nodes: nodes.ToGraphNodeArray(),
            infoToAgv: new Dictionary<InfoToAgvKey, string>
            {
                { InfoToAgvKey.DockFront, "true" }
            });

        var routePlan = new RoutePlan(nodes.SkipLast(1).ToArray().ToGraphNodeArray(), [], [], []);
        var cmd = new AbortAndDispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, action, routePlan, abortedId);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<AbortAndDispatchRouteCommand, MqttOrder>(cmd);

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
    public async Task Consume_ResendDispatchedRouteCommand_ValidData_PublishesMqttOrder()
    {
        var action = AgvActionGenerator.GenerateAction(actionType: ActionType.Pick, targetNodeId: ToStation);
        var fixture = new ActionCommandConsumerTestFixture();
        var resend = new ResendDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<ResendDispatchedRouteCommand, MqttOrder>(resend);

        mqtt.OrderId.ShouldBe(action.CorrelationId.ToString());
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
    }

    [TestMethod]
    public async Task Consume_ResendDispatchedRouteCommand_UpdatedOrder_PublishesMqttOrder()
    {
        var vehicleOrderId = Guid.NewGuid();
        var action = AgvActionGenerator.GenerateAction(actionType: ActionType.Pick, targetNodeId: ToStation);
        var fixture = new ActionCommandConsumerTestFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(vehicleOrderId, action.CorrelationId);

        var resend = new ResendDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), action);

        var mqtt = await fixture.PublishAndGetFirstMessageAsync<ResendDispatchedRouteCommand, MqttOrder>(resend);

        mqtt.OrderId.ShouldBe(vehicleOrderId.ToString());
        mqtt.Stations.ShouldNotBeNull();
        mqtt.Stations.FromStation.ShouldNotBeNull();
        mqtt.Stations.FromStation.Id.ShouldBe(ToStation);
        mqtt.SerialNumber.ShouldBe(AgvSerialNumber);
    }

    [TestMethod]
    public async Task Consume_DispatchRouteCommand_PickDrop_HasRightOrderIds()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var pickAction = actionGroup.Actions.First();
        var dropAction = actionGroup.Actions.Last();

        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), pickAction);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);
        mqtt.OrderId.ShouldBe(pickAction.CorrelationId.ToString());

        command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), dropAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command, 2);
        mqtt.OrderId.ShouldBe(dropAction.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_DispatchRouteCommand_MovePickDrop_HasRightOrderIds()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        var actionGroup = AgvActionGenerator.GenerateMovePickDropActionGroup();
        var moveAction = actionGroup.Actions.First();
        var pickAction = actionGroup.Actions.Skip(1).First();
        var dropAction = actionGroup.Actions.Skip(2).First();

        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), moveAction);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);
        mqtt.OrderId.ShouldBe(moveAction.CorrelationId.ToString());

        command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), pickAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command, 2);
        mqtt.OrderId.ShouldBe(pickAction.CorrelationId.ToString());

        command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), dropAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command, 3);
        mqtt.OrderId.ShouldBe(dropAction.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_DispatchRouteCommand_PickMoveDrop_HasRightOrderIds()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickMoveDropActionGroup();
        var pickAction = actionGroup.Actions.First();
        var moveAction = actionGroup.Actions.Skip(1).First();
        var dropAction = actionGroup.Actions.Skip(2).First();

        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), pickAction);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);
        mqtt.OrderId.ShouldBe(pickAction.CorrelationId.ToString());

        command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), moveAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command, 2);
        mqtt.OrderId.ShouldBe(moveAction.CorrelationId.ToString());

        command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), dropAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command, 3);
        mqtt.OrderId.ShouldBe(dropAction.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_DispatchRouteCommandAndUpdateDispatchedRouteCommand_PickAndMoveGetsUpdatedThenDrop_HasRightOrderIds()
    {
        var fixture = new ActionCommandConsumerTestFixture();
        var actionGroup = AgvActionGenerator.GeneratePickMoveDropActionGroup();
        var pickAction = actionGroup.Actions.First();
        var moveAction = actionGroup.Actions.Skip(1).First();
        var dropAction = actionGroup.Actions.Skip(2).First();

        var command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), pickAction);
        var mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command);
        mqtt.OrderId.ShouldBe(pickAction.CorrelationId.ToString());
        await fixture.TryRemoveMappingAsync(mqtt.OrderId.ToGuid(), pickAction.CorrelationId);

        command = new DispatchRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), moveAction);
        mqtt = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, MqttOrder>(command, 2);
        mqtt.OrderId.ShouldBe(moveAction.CorrelationId.ToString());

        var updateCommand = new UpdateDispatchedRouteCommand(_agvCorrelationId, AgvSerialNumber, new RoutePlan([], [], [], []), dropAction);
        await fixture.TestHarness.Bus.Publish(updateCommand);
        mqtt = await fixture.GetFirstPublishedMessageAsync<MqttOrder>(3);
        mqtt.OrderId.ShouldBe(dropAction.CorrelationId.ToString());
    }
}