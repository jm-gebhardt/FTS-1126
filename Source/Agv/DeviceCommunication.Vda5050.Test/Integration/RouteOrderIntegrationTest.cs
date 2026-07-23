using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.Common.Messages.Route.AgvActions;
using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;
using MassTransit;
using Shouldly;
using RouteStep = Gebhardt.Acc.Common.Messages.Route.RouteSteps.RouteStep;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Integration;

[TestClass]
[DoNotParallelize]
public class RouteOrderIntegrationTest
{
    private const string StartNodeId = "start";
    private const string MidNodeId = "mid";
    private const string EndNodeId = "end";
    private const string FarNodeId = "far";

    private readonly IntegrationTestFixture _fixture = new();
    
    private readonly Agv _agv = AgvGenerator.Generate("agv1".ToGuid());

    public RouteOrderIntegrationTest()
    {
        _fixture.AddAgv(_agv);
        _fixture.AddPickAction(_agv.SerialNumber);
        _fixture.AddLayout(_agv.VehicleTypeId, BuildLayout(_agv.VehicleTypeId));
    }

    private static Layout BuildLayout(string vehicleTypeId)
    {
        var pickProperty = new VehicleTypeNodeProperty(Guid.NewGuid(), vehicleTypeId,
        [
            new VehicleTypeNodePropertyAction(Vda5050Specifications.ActionTypes.Pick, BlockingType.HARD,
                requirementType: RequirementType.Required,
                actionParameters: [new NodeActionParameter(Vda5050Specifications.Parameters.StationType, "PICK")])
        ]);
        var startNode = new Node(StartNodeId, new NodePosition(0, 0), []);
        var midNode = new Node(MidNodeId, new NodePosition(1, 0), []);
        var endNode = new Node(EndNodeId, new NodePosition(2, 0), [pickProperty]);
        var farNode = new Node(FarNodeId, new NodePosition(3, 0), []);
        var edge1 = new Edge("edge1", endNodeId: MidNodeId, startNodeId: StartNodeId, [new VehicleTypeEdgeProperty(Guid.NewGuid(), vehicleTypeId)]);
        var edge2 = new Edge("edge2", endNodeId: EndNodeId, startNodeId: MidNodeId, [new VehicleTypeEdgeProperty(Guid.NewGuid(), vehicleTypeId)]);
        var edge3 = new Edge("edge3", endNodeId: FarNodeId, startNodeId: EndNodeId, [new VehicleTypeEdgeProperty(Guid.NewGuid(), vehicleTypeId)]);
        return new Layout(Guid.NewGuid(), "layout-1", "1.0",
            [edge1, edge2, edge3], [startNode, midNode, endNode, farNode], []);
    }

    private async Task<MqttOrder> PlanAndReleaseRouteAsync(Guid routeId, RouteStep[] steps)
    {
        await _fixture.PublishAndConsumeAsync(new RoutePlanned(routeId, _agv.CorrelationId, steps));
        await _fixture.PublishAndConsumeAsync(new RouteStepsReleased(routeId, _agv.CorrelationId, steps.Select(s => s.RouteStepId).ToArray()));
        return await _fixture.GetFirstPublishedMessageAsync<MqttOrder>();
    }

    [TestMethod]
    public async Task RoutePlanned_ThenReleased_PublishesMqttOrderWithFullRoute()
    {
        var routeId = Guid.NewGuid();
        RouteStep[] steps = RouteStepGenerator.GenerateFromNodeIds([StartNodeId, MidNodeId, EndNodeId], new PickAction(null));

        await _fixture.PublishAndConsumeAsync(new RoutePlanned(routeId, _agv.CorrelationId, steps));
        // RoutePlanned is already confirmed fully consumed above, so a snapshot check suffices here —
        // ShouldNotHavePublishedAsync<T>() would instead wait out the harness's one-shot
        // TestInactivityTimeout (~6s) to prove the negative, permanently cancelling it and silently
        // breaking every later Any<T>() check in this test (including genuine, successful consumption).
        _fixture.GetPublishedMessages<MqttOrder>().ShouldBeEmpty();

        await _fixture.PublishAndConsumeAsync(new RouteStepsReleased(routeId, _agv.CorrelationId, steps.Select(s => s.RouteStepId).ToArray()));
        var mqttOrder = await _fixture.GetFirstPublishedMessageAsync<MqttOrder>();

        mqttOrder.Nodes.Count.ShouldBe(3);
        mqttOrder.Edges.Count.ShouldBe(2);
        mqttOrder.Nodes.ShouldAllBe(n => n.Released);
        mqttOrder.Edges.ShouldAllBe(e => e.Released);
        var endNode = mqttOrder.Nodes.Single(n => n.NodeId == EndNodeId);
        endNode.Actions.ShouldHaveSingleItem();
        endNode.Actions.Single().ActionType.ShouldBe(Vda5050Specifications.ActionTypes.Pick);
        mqttOrder.HeaderId.ShouldBe(1u);
        mqttOrder.OrderUpdateId.ShouldBe(0);
    }

    [TestMethod]
    public async Task MqttState_ReportsOrderComplete_PublishesRouteStepsProcessed()
    {
        var routeId = Guid.NewGuid();
        RouteStep[] steps = RouteStepGenerator.GenerateFromNodeIds([StartNodeId, MidNodeId, EndNodeId], new PickAction(null));
        var mqttOrder = await PlanAndReleaseRouteAsync(routeId, steps);
        var endNode = mqttOrder.Nodes.Single(n => n.NodeId == EndNodeId);
        string pickActionId = endNode.Actions.Single().ActionId;

        var state = MqttStateGenerator.Generate(
            serialNumber: _agv.SerialNumber,
            actionStates: [MqttStateGenerator.GenerateActionState(pickActionId, MqttActionStatus.Finished)]);
        state.OrderId = mqttOrder.OrderId;
        state.OrderUpdateId = mqttOrder.OrderUpdateId;
        state.LastNodeSequenceId = endNode.SequenceId;

        await _fixture.PublishMqttAndConsumeAsync(state, "routing.key");

        var processed = await _fixture.GetFirstPublishedMessageAsync<RouteStepsProcessed>();
        processed.RouteStepStates.Length.ShouldBe(2);
        processed.RouteStepStates.ShouldAllBe(s => s.Status == RouteStepStatus.Completed);
        _fixture.GetRouteExecution(routeId).CurrentOrderCompleted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RouteUpdated_ExtendsRoute_PublishesUpdatedMqttOrder()
    {
        var routeId = Guid.NewGuid();
        RouteStep[] steps = RouteStepGenerator.GenerateFromNodeIds([StartNodeId, MidNodeId, EndNodeId], new PickAction(null));
        var firstOrder = await PlanAndReleaseRouteAsync(routeId, steps);

        RouteStep[] extendedSteps = RouteStepGenerator.AppendStep(steps, FarNodeId);
        await _fixture.PublishAndConsumeAsync(new RouteUpdated(routeId, _agv.CorrelationId, extendedSteps));

        var secondOrder = await _fixture.GetFirstPublishedMessageAsync<MqttOrder>(occurrenceNumber: 2);
        secondOrder.HeaderId.ShouldBe(2u);
        secondOrder.OrderUpdateId.ShouldBe(firstOrder.OrderUpdateId + 1);
        secondOrder.Nodes.Select(n => n.NodeId).ShouldBe([StartNodeId, MidNodeId, EndNodeId, FarNodeId], ignoreOrder: true);
        secondOrder.Edges.Count.ShouldBe(3);
        // The extended step (end -> far) was never released via RouteStepsReleased, so it stays horizon.
        secondOrder.Nodes.Where(n => n.NodeId != FarNodeId).ShouldAllBe(n => n.Released);
        secondOrder.Nodes.Single(n => n.NodeId == FarNodeId).Released.ShouldBeFalse();
        secondOrder.Edges.Where(e => e.EndNodeId != FarNodeId).ShouldAllBe(e => e.Released);
        secondOrder.Edges.Single(e => e.EndNodeId == FarNodeId).Released.ShouldBeFalse();
    }

    [TestMethod]
    public async Task RouteAborted_ActiveOrder_PublishesCancelOrderInstantAction()
    {
        var routeId = Guid.NewGuid();
        RouteStep[] steps = RouteStepGenerator.GenerateFromNodeIds([StartNodeId, MidNodeId, EndNodeId], new PickAction(null));
        await PlanAndReleaseRouteAsync(routeId, steps);

        await _fixture.PublishAndConsumeAsync(new RouteAborted(routeId, _agv.CorrelationId));

        var instantActions = await _fixture.GetFirstPublishedMessageAsync<MqttInstantActions>();
        instantActions.SerialNumber.ShouldBe(_agv.SerialNumber);
        instantActions.Actions.ShouldHaveSingleItem();
        var cancelAction = instantActions.Actions.Single();
        cancelAction.ActionType.ShouldBe(Vda5050Specifications.ActionTypes.CancelOrder);
        cancelAction.BlockingType.ShouldBe(BlockingType.HARD);

        string? routingKey = _fixture.TestHarness.Published.Select<MqttInstantActions>().First().Context.RoutingKey();
        routingKey.ShouldBe(_agv.GetRoutingKey(Vda5050Specifications.Subtopics.InstantActions));
    }

    [TestMethod]
    public async Task RouteStepsReleased_MixedWithPreviouslyReleasedSteps_OrderUpdateOnlyContainsStitchingNodeOnward()
    {
        var routeId = Guid.NewGuid();
        RouteStep[] steps = RouteStepGenerator.GenerateFromNodeIds([StartNodeId, MidNodeId, EndNodeId, FarNodeId]);

        await _fixture.PublishAndConsumeAsync(new RoutePlanned(routeId, _agv.CorrelationId, steps));
        await _fixture.PublishAndConsumeAsync(new RouteStepsReleased(routeId, _agv.CorrelationId, [steps[0].RouteStepId]));
        var firstOrder = await _fixture.GetFirstPublishedMessageAsync<MqttOrder>();
        var midNode = firstOrder.Nodes.Single(n => n.NodeId == MidNodeId);

        // AGV reports it has reached the end of the first released step - this becomes the stitching node.
        var state = MqttStateGenerator.Generate(serialNumber: _agv.SerialNumber);
        state.OrderId = firstOrder.OrderId;
        state.OrderUpdateId = firstOrder.OrderUpdateId;
        state.LastNodeSequenceId = midNode.SequenceId;
        await _fixture.PublishMqttAndConsumeAsync(state, "routing.key");
        await _fixture.WaitUntilConsumedAsync<OrderStateReceived>();

        // Release the next step, re-submitting the already-released first step's id alongside it.
        await _fixture.PublishAndConsumeAsync(new RouteStepsReleased(routeId, _agv.CorrelationId, [steps[0].RouteStepId, steps[1].RouteStepId]));

        var secondOrder = await _fixture.GetFirstPublishedMessageAsync<MqttOrder>(occurrenceNumber: 2);
        secondOrder.HeaderId.ShouldBe(2u);
        secondOrder.OrderUpdateId.ShouldBe(firstOrder.OrderUpdateId + 1);

        // The already-sent base (start node and its edge) must not be resent - only the stitching
        // node (mid, the AGV's last reported position) onward is included.
        secondOrder.Nodes.Select(n => n.NodeId).ShouldBe([MidNodeId, EndNodeId, FarNodeId]);
        secondOrder.Nodes.Single(n => n.NodeId == MidNodeId).SequenceId.ShouldBe(midNode.SequenceId);
        secondOrder.Edges.Count.ShouldBe(2);
        secondOrder.Edges.ShouldNotContain(e => e.StartNodeId == StartNodeId);
    }
}
