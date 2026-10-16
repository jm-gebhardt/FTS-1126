using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Commands;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation.Travel;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class SimulatedAgvStateMachineTest
{
    [TestMethod]
    public async Task When_RequestAgvGraph_AgvGraphReceived_isPublished()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph,
            dataExists: false
        );
        var correlationId = agvSaga.CorrelationId;
        string agvSerialNumber = agvSaga.SerialNumber;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        var message = await fixture.PublishAndGetFirstMessageAsync<RequestAgvGraph, AgvGraphReceived>(new RequestAgvGraph(correlationId, agvSerialNumber));
        (await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();
        message.CorrelationId.ShouldBe(correlationId);
        message.SerialNumber.ShouldBe(agvSerialNumber);
        message.Graph.ShouldBe(graph);
    }

    [TestMethod]
    public async Task When_RequestAgvGraph_AgvGraphIsNull_NoMessageIsPublished()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph,
            graphExists: false
        );
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        // Publish RequestAgvGraph ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvSaga.SerialNumber));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();
        // ... but don't expect AgvGraphReceived being published.
        var token = new CancellationTokenSource();
        token.CancelAfter(100);
        await fixture.ShouldNotHavePublishedAsync<AgvGraphReceived>();
    }

    [TestMethod]
    public async Task Consume_RequestAgvZones_ValidData_PublishesAgvZonesReceived()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph,
            dataExists: false
        );
        var correlationId = agvSaga.CorrelationId;
        string agvSerialNumber = agvSaga.SerialNumber;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        var message = await fixture.PublishAndGetFirstMessageAsync<RequestAgvZones, AgvZonesReceived>(new RequestAgvZones(correlationId, agvSerialNumber));
        (await fixture.SagaHarness.Consumed.Any<RequestAgvZones>()).ShouldBeTrue();
        message.SerialNumber.ShouldBe(agvSerialNumber);
        message.CorrelationId.ShouldBe(correlationId);
        message.AgvZones.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task Consume_RequestAgvZones_NoGraph_DoesNotPublishAgvZonesReceived()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph,
            graphExists: false
        );
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        // Publish RequestAgvGraph ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvZones(correlationId, agvSaga.SerialNumber));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvZones>()).ShouldBeTrue();
        // ... but don't expect AgvZonesReceived being published.
        var token = new CancellationTokenSource();
        token.CancelAfter(100);
        await fixture.ShouldNotHavePublishedAsync<AgvZonesReceived>();
    }

    [TestMethod]
    public async Task When_RequestAgvData_AgvDataIsNull_NoMessageIsPublished()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph,
            dataExists: false
        );
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        // Publish RequestAgvData ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvSaga.SerialNumber));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvData>()).ShouldBeTrue();
        // ... but don't expect AgvDataReceived being published.
        var token = new CancellationTokenSource();
        token.CancelAfter(100);
        (await fixture.TestHarness.Published.Any<AgvDataReceived>(token.Token)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task When_RequestAgvData_AgvDataReceived_isPublished()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph
        );
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        string agvSerialNumber = agvSaga.SerialNumber;
        var message = await fixture.PublishAndGetFirstMessageAsync<RequestAgvData, AgvDataReceived>(
            new RequestAgvData(correlationId, agvSerialNumber));
        (await fixture.SagaHarness.Consumed.Any<RequestAgvData>()).ShouldBeTrue();

        message.CorrelationId.ShouldBe(correlationId);
        message.SerialNumber.ShouldBe(agvSerialNumber);
        message.Data.ShouldBe(agvSaga.AgvData);
    }

    [TestMethod]
    public async Task When_StartActionCommand_ActionStartedByAgv_IsPublished_And_ActionCompletedByAgv_Is_Published()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        ICollection<GraphNode> nodes = graph.Nodes;
        var action = AgvActionGenerator.GeneratePickDropActionGroup().Actions.First();
        (GraphNode[] nodes, Guid[] edges) route = AgvGraphGenerator.CreateRoute(action.Target.Node);
        var routePlan = RoutePlanGenerator.Build(route.nodes, route.edges);
        var startNode = nodes.First(n => n.Id == route.nodes.First().Id);
        var destination = route.nodes.Last();
        // Keep the simulated run short so the test remains fast in CI while still verifying
        // that the action starts and completes end-to-end.
        var agvSaga = SimulatedAgvSagaGenerator.Generate(simulationSpeed: 20, agvGraph: graph, currentNode: startNode);
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        var actionStarted = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, ActionStartedByAgv>(
            new DispatchRouteCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, routePlan, action));

        var actionId = action.CorrelationId;
        actionStarted.CorrelationId.ShouldBe(actionId);

        // Position sent (Node travel time is twice as long as position sent interval, so this is surely sent in the meanwhile)
        (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();

        // action finished
        (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();

        //wait until saga is updated
        await fixture.WaitUntil(agvSaga, s =>
            {
                bool ok = !s.AgvStatus.Busy;
                ok &= s.LastNodeId == destination.Id;
                return ok;
            },
            s =>
            {
                s.AgvStatus.Busy.ShouldBeFalse();
                s.LastNodeId.ShouldBe(destination.Id);
            },
            timeoutInMs: 1000);

        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();

        await fixture.ShouldHavePublishedAsync<ActionCompletedByAgv>();
        var actionCompleted = await fixture.GetFirstPublishedMessageAsync<ActionCompletedByAgv>();
        actionCompleted.CorrelationId.ShouldBe(actionId);

        // Last position message received should be the position of the last node
        await fixture.ShouldHavePublishedAsync<AgvPositionReceived>();
        var destinationNode = nodes.First(n => n.Name == destination.Name);
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.X - destinationNode.X) < 0.01)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Y - destinationNode.Y) < 0.01)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Z - destinationNode.Z) < 0.01)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_UpdateDispatchedRouteCommand_InStatusBusyMoving_PublishesActionUpdatedByAgv()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();

        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget();
        var moveAction = actionGroup.Actions.Skip(1).First();
        var dropAction = actionGroup.Actions.Last();

        // Inject the saga directly into BusyMoving so the update is published into a quiet harness.
        // Without a preceding StartActionCommand there are no self-rescheduling position/status/finish
        // schedules running, so the saga consumes the update immediately instead of racing a backlog of
        // scheduled messages. That backlog under CI load was the cause of the previous flakiness.
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentState: nameof(SimulatedAgvStateMachine.BusyMoving),
            currentAction: moveAction,
            agvGraph: graph);
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(6))
            .WithAgv(agvSaga)
            .Build();

        // Update the in-progress move to a drop action with a new (far) target.
        (ICollection<GraphNode> dropNodes, ICollection<Guid> dropEdges) dropRoute = AgvGraphGenerator.CreateRoute(AgvGraphGenerator.DestinationFar);
        List<GraphNode> updateNodes = dropRoute.dropNodes.Skip(1).ToList();
        List<Guid> updateEdges = dropRoute.dropEdges.Skip(1).ToList();
        var updatedMoveAction = AgvActionGenerator.GenerateAction(
            correlationId: moveAction.CorrelationId,
            actionType: dropAction.ActionType,
            targetNodeId: AgvGraphGenerator.DestinationFar.Name,
            parentId: dropAction.ParentId,
            infoToAgv: dropAction.InfoToAgv,
            nodes: updateNodes.ToArray(),
            edges: updateEdges.ToArray());
        var updateRoutePlan = RoutePlanGenerator.Build(updateNodes, updateEdges);

        // The saga consumes the update while BusyMoving and publishes ActionUpdatedByAgv.
        var actionUpdated = await fixture.PublishAndGetFirstMessageAsync<UpdateDispatchedRouteCommand, ActionUpdatedByAgv>(
            new UpdateDispatchedRouteCommand(correlationId, agvSaga.SerialNumber, updateRoutePlan, updatedMoveAction));
        actionUpdated.CorrelationId.ShouldBe(updatedMoveAction.CorrelationId);

        // The update was applied: the saga now targets the new (far) destination.
        // The end-to-end "redirect and complete at the new target" path is covered by
        // When_UpdateActionCommand_ActionAlreadyFinished_IsPublished_And_ActionCompletedByAgv_Is_Published.
        agvSaga.Action!.CorrelationId.ShouldBe(updatedMoveAction.CorrelationId);
        agvSaga.Action.Target.Node?.Name.ShouldBe(AgvGraphGenerator.DestinationFar.Name);
    }

    [TestMethod]
    public async Task When_UpdateActionCommand_ActionAlreadyFinished_IsPublished_And_ActionCompletedByAgv_Is_Published()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();

        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget();
        var moveAction = actionGroup.Actions.Skip(1).FirstOrDefault();
        (GraphNode[] nodes, Guid[] edges) route = AgvGraphGenerator.CreateRoute(moveAction!.Target.Node);
        var startRoutePlan = RoutePlanGenerator.Build(route.nodes, route.edges);

        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentState: nameof(SimulatedAgvStateMachine.Idle),
            currentAction: moveAction,
            agvGraph: graph,
            simulationSpeed: 2);
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(6))
            .WithAgv(agvSaga)
            .Build();

        // Move action will be updated to Drop action.
        var moveActionToDropAction = actionGroup.Actions.LastOrDefault();
        if (moveActionToDropAction != null)
        {
            (ICollection<GraphNode> dropNodes, ICollection<Guid> dropEdges) dropRoute = AgvGraphGenerator.CreateRoute(AgvGraphGenerator.DestinationFar);
            List<GraphNode> updateNodes = dropRoute.dropNodes.Skip(1).ToList();
            List<Guid> updateEdges = dropRoute.dropEdges.Skip(1).ToList();
            var updatedMoveAction = AgvActionGenerator.GenerateAction(
                correlationId: moveAction.CorrelationId,
                actionType: moveActionToDropAction.ActionType,
                targetNodeId: AgvGraphGenerator.DestinationFar.Name,
                parentId: moveActionToDropAction.ParentId,
                infoToAgv: moveActionToDropAction.InfoToAgv,
                nodes: updateNodes.ToArray(),
                edges: updateEdges.ToArray()) with { Revision = 1 };
            var updateRoutePlan = RoutePlanGenerator.Build(updateNodes, updateEdges);

            // Start move action
            await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, ActionStartedByAgv>(
                new DispatchRouteCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, startRoutePlan, moveAction));

            var moveCompleted = await fixture.GetFirstPublishedMessageAsync<ActionCompletedByAgv>();
            moveCompleted.Revision.ShouldBe(0);

            // Update move action to drop action with new target
            var actionUpdated = await fixture.PublishAndGetFirstMessageAsync<UpdateDispatchedRouteCommand, ActionUpdatedByAgv>(
                new UpdateDispatchedRouteCommand(correlationId, agvSaga.SerialNumber, updateRoutePlan, updatedMoveAction));
            actionUpdated.CorrelationId.ShouldBe(updatedMoveAction.CorrelationId);

            // Position sent (Node travel time is twice as long as position sent interval, so this is surely sent in the meanwhile)
            (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();

            // action finished
            (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<DispatchRouteCommand>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<UpdateDispatchedRouteCommand>()).ShouldBeTrue();

            // Wait until saga has finished the updated action before asserting publication
            await fixture.WaitUntil(agvSaga, s =>
                {
                    bool ok = !s.AgvStatus.Busy;
                    ok &= s.LastNodeId == updatedMoveAction.Target.Node?.Id;
                    return ok;
                },
                s =>
                {
                    s.AgvStatus.Busy.ShouldBeFalse();
                    s.LastNodeId.ShouldBe(updatedMoveAction.Target.Node?.Id);
                }, timeoutInMs: 10000);

            (await fixture.TestHarness.Published.Any<ActionCompletedByAgv>(c => c.Context.Message.Revision == 1)).ShouldBeTrue();
        }
    }

    [TestMethod]
    public async Task When_PickActionFinished_PublishedAgvLoadStatusReceivedWithOneCargo()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph
        );
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();
        agvSaga.TravelSimulator.GetType().GetProperty(nameof(AgvTravelSimulator.CurrentNode))!.SetValue(agvSaga.TravelSimulator, node);
        var action = AgvActionGenerator.GenerateAction(startNodeId: node.Name, targetNodeId: node.Name, actionType: ActionType.Pick);
        var routePlan = new RoutePlan([node], [], [], []);
        var agvLoadStatusReceived = await fixture.PublishAndGetFirstMessageAsync<DispatchRouteCommand, AgvLoadStatusReceived>(
            new DispatchRouteCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, routePlan, action));

        await fixture.WaitUntilAgvIdle(agvSaga);
        agvLoadStatusReceived.Cargos.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task When_DropActionFinished_PublishedAgvLoadStatusReceivedWithNoCargo()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph
        );
        agvSaga.TravelSimulator.GetType().GetProperty(nameof(AgvTravelSimulator.CurrentNode))!.SetValue(agvSaga.TravelSimulator, node);
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();
        var action = AgvActionGenerator.GenerateAction(startNodeId: node.Name, targetNodeId: node.Name, actionType: ActionType.Drop);
        var routePlan = new RoutePlan([node], [], [], []);

        await fixture.PublishMessageAsync(new DispatchRouteCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, routePlan, action));
        await fixture.WaitUntilAgvIdle(agvSaga);
        var agvLoadStatusReceived = await fixture.GetFirstPublishedMessageAsync<AgvLoadStatusReceived>();
        agvLoadStatusReceived.Cargos.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task Consume_ActionAborted_InStatusBusyMoving_TransitionsToIdlePublishesActionAbortedByAgv()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var action = AgvActionGenerator.GenerateAction(startNodeId: graph.Nodes.First().Name, targetNodeId: graph.Nodes.Skip(1).First().Name);
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentState: nameof(SimulatedAgvStateMachine.BusyMoving),
            currentAction:
            action,
            agvGraph:
            graph);
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(6))
            .WithAgv(agvSaga)
            .Build();

        await fixture.PublishMessageAsync(new ActionAborted(correlationId, action));

        await fixture.ShouldHavePublishedAsync<ActionAbortedByAgv>();
        agvSaga.ActionStartDate.ShouldBeNull();
        agvSaga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        agvSaga.LastActionAborted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ChargeActionAborted_InStatusBusyMoving_TransitionsToIdlePublishesActionAbortedByAgv()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var action = AgvActionGenerator.GenerateChargeAction(startNodeId: graph.Nodes.First().Name, targetNodeId: graph.Nodes.Skip(1).First().Name);
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentState: nameof(SimulatedAgvStateMachine.BusyMoving),
            currentAction:
            action,
            agvGraph:
            graph);
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(6))
            .WithAgv(agvSaga)
            .Build();

        await fixture.PublishMessageAsync(new ActionAborted(correlationId, action));

        await fixture.ShouldHavePublishedAsync<ActionAbortedByAgv>();
        agvSaga.ActionStartDate.ShouldBeNull();
        agvSaga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        agvSaga.LastActionAborted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ScheduleFinishAction_AfterActionWasAborted_ResetLastActionAborted()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var action = AgvActionGenerator.GenerateAction(startNodeId: graph.Nodes.First().Name, targetNodeId: graph.Nodes.Skip(1).First().Name);
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentState: nameof(SimulatedAgvStateMachine.BusyMoving),
            currentAction:
            action,
            agvGraph:
            graph);
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(6))
            .WithAgv(agvSaga)
            .Build();

        await fixture.PublishMessageAsync(new ActionAborted(correlationId, action));
        await fixture.ShouldHavePublishedAsync<ActionAbortedByAgv>();
        agvSaga.ActionStartDate.ShouldBeNull();
        agvSaga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        agvSaga.LastActionAborted.ShouldBeTrue();

        await fixture.PublishMessageAsync(new SimulatedAgvStateMachine.ScheduleFinishAction(correlationId));
    }

    [TestMethod]
    public async Task Consume_SimulateAgvError_PublishesAgvErrorStatusReceived()
    {
        var agvSaga = SimulatedAgvSagaGenerator.Generate();
        var correlationId = agvSaga.CorrelationId;
        var agvError = AgvErrorGenerator.GenerateAgvError(correlationId);
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        var message = await fixture.PublishAndGetFirstMessageAsync<SimulateAgvError, AgvErrorStatusReceived>(
            new SimulateAgvError(correlationId, new List<AgvError> { agvError }));

        (await fixture.SagaHarness.Consumed.Any<SimulateAgvError>()).ShouldBeTrue();

        message.CorrelationId.ShouldBe(correlationId);
        message.Errors.ShouldNotBeNull();
        message.Errors.Count.ShouldBe(1);
        message.Errors.First().ShouldSatisfyAllConditions(
            e => e.ErrorDescription.ShouldBe(agvError.ErrorDescription),
            e => e.ErrorLevel.ToString().ShouldBe(agvError.ErrorLevel.ToString()),
            e => e.ErrorType.ShouldBe(agvError.ErrorType),
            e => e.ErrorReferences.ShouldNotBeNull(),
            e => e.ErrorReferences!.First().ShouldSatisfyAllConditions(
                r => r.ReferenceKey.ShouldBe(agvError.ErrorReferences!.First().ReferenceKey),
                r => r.ReferenceValue.ShouldBe(agvError.ErrorReferences!.First().ReferenceValue)));
    }

    [TestMethod]
    public async Task Consume_SimulateAgvError_CallsTravelSimulatorPause_WhenErrorAdded()
    {
        var agvSaga = SimulatedAgvSagaGenerator.Generate();
        var correlationId = agvSaga.CorrelationId;
        var agvError = AgvErrorGenerator.GenerateAgvError(correlationId);
        var agvTravelSimulatorMock = new AgvTravelSimulatorMock();
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .WithAgvTravelSimulator(agvTravelSimulatorMock.Object)
            .Build();

        await fixture.PublishMessageAsync(new SimulateAgvError(correlationId, new List<AgvError> { agvError }));

        agvTravelSimulatorMock.Mock.Verify(m => m.Pause(), Times.Once);
    }

    [TestMethod]
    public async Task Consume_ResetAgvErrors_CallsTravelSimulatorResume_WhenErrorRemoved()
    {
        var agvSaga = SimulatedAgvSagaGenerator.Generate();
        var correlationId = agvSaga.CorrelationId;
        var agvError = AgvErrorGenerator.GenerateAgvError(correlationId);
        var agvTravelSimulatorMock = new AgvTravelSimulatorMock();
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .WithAgvTravelSimulator(agvTravelSimulatorMock.Object)
            .Build();

        await fixture.PublishMessageAsync(new SimulateAgvError(correlationId, new List<AgvError> { agvError }));
        await fixture.PublishMessageAsync(new ResetAgvErrors(correlationId, []));

        agvTravelSimulatorMock.Mock.Verify(m => m.Resume(), Times.Once);
    }
}