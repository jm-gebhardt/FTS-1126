using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
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
    public async Task When_StartActionCommand_ActionStarted_IsPublished_And_ActionCompleted_Is_Published()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        ICollection<GraphNode> nodes = graph.Nodes;
        var action = AgvActionGenerator.GeneratePickDropActionGroup().Actions.First();
        (string[] nodes, Guid[] edges) route = AgvGraphGenerator.CreateRoute(action.Target.NodeId!);
        action.Nodes = route.nodes;
        action.Edges = route.edges;
        var startNode = nodes.First(n => n.Id == action.Nodes.First());
        string destination = action.Nodes.Last();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(simulationSpeed: 2, agvGraph: graph, currentNode: startNode);
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        var actionStarted = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, ActionStarted>(
            new StartActionCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, action));

        var actionId = action.CorrelationId;
        actionStarted.CorrelationId.ShouldBe(actionId);

        agvSaga.AgvStatus.Busy.ShouldBeTrue();
        agvSaga.ActionStartDate.ShouldNotBeNull();

        // Position sent (Node travel time is twice as long as position sent interval, so this is surely sent in the meanwhile)
        (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();

        // action finished
        (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();

        //wait until saga is updated
        await fixture.WaitUntil(agvSaga, s =>
            {
                bool ok = !s.AgvStatus.Busy;
                ok &= s.LastNodeId == destination;
                return ok;
            },
            s =>
            {
                s.AgvStatus.Busy.ShouldBeFalse();
                s.LastNodeId.ShouldBe(destination);
            });

        await fixture.ShouldHavePublishedAsync<ActionCompleted>();
        var actionCompleted = await fixture.GetFirstPublishedMessageAsync<ActionCompleted>();
        actionCompleted.CorrelationId.ShouldBe(actionId);

        // Last position message received should be the position of the last node
        var destinationNode = nodes.First(n => n.Id == destination);
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.X - destinationNode.X) < 0.01)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Y - destinationNode.Y) < 0.01)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Z - destinationNode.Z) < 0.01)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task When_UpdateActionCommand_ActionUpdated_IsPublished_And_ActionCompleted_Is_Published()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();

        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget();
        var moveAction = actionGroup.Actions.Skip(1).FirstOrDefault();
        (string[] nodes, Guid[] edges) route = AgvGraphGenerator.CreateRoute(moveAction!.Target.NodeId!);
        moveAction.Nodes = route.nodes;
        moveAction.Edges = route.edges;

        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentState: nameof(SimulatedAgvStateMachine.Idle),
            currentAction:
            moveAction,
            agvGraph:
            graph);
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(6))
            .WithAgv(agvSaga)
            .Build();

        // Move action will be updated to Drop action. 
        var moveActionToDropAction = actionGroup.Actions.LastOrDefault();
        if (moveActionToDropAction != null)
        {
            (ICollection<string> dropNodes, ICollection<Guid> dropEdges) dropRoute = AgvGraphGenerator.CreateRoute(AgvGraphGenerator.DestinationFar.Id);
            var updatedMoveAction = new AgvAction(moveAction.CorrelationId, moveActionToDropAction.ActionType, AgvGraphGenerator.DestinationFar.Id, moveActionToDropAction.ParentId, moveActionToDropAction.InfoToAgv)
            {
                // Skip on node to simulate running AGV
                Nodes = dropRoute.dropNodes.Skip(1).ToArray(),
                Edges = dropRoute.dropEdges.Skip(1).ToArray()
            };

            // Start move action 
            await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, ActionStarted>(
                new StartActionCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, moveAction));

            //wait until saga is updated
            await fixture.WaitUntil(agvSaga, s => s.AgvStatus.Busy, s => s.AgvStatus.Busy.ShouldBeTrue());

            // Update move action to drop action with new target
            var actionUpdated = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, ActionUpdated>(
                new UpdateActionCommand(correlationId, agvSaga.SerialNumber, updatedMoveAction));
            actionUpdated.CorrelationId.ShouldBe(updatedMoveAction.CorrelationId);

            // Position sent (Node travel time is twice as long as position sent interval, so this is surely sent in the meanwhile)
            (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();

            // action finished
            (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();

            //wait until saga is updated
            await fixture.WaitUntil(agvSaga, s =>
                {
                    bool ok = !s.AgvStatus.Busy;
                    ok &= s.LastNodeId == updatedMoveAction.Target.NodeId;
                    return ok;
                },
                s =>
                {
                    Console.WriteLine(s.AgvStatus);
                    s.AgvStatus.Busy.ShouldBeFalse();
                    s.LastNodeId.ShouldBe(updatedMoveAction.Target.NodeId);
                }, timeoutInMs: 10000);

            await fixture.ShouldHavePublishedAsync<ActionCompleted>();
            var actionCompleted = await fixture.GetFirstPublishedMessageAsync<ActionCompleted>();
            actionCompleted.CorrelationId.ShouldBe(updatedMoveAction.CorrelationId);

            // Last position message received should be the position of the last node
            var destinationNode = graph.Nodes.First(n => n.Id == updatedMoveAction.Target.NodeId);
            (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.X - destinationNode.X) < 0.01)).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Y - destinationNode.Y) < 0.01)).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Z - destinationNode.Z) < 0.01)).ShouldBeTrue();
        }
    }

    [TestMethod]
    public async Task When_UpdateActionCommand_ActionAlreadyFinished_IsPublished_And_ActionCompleted_Is_Published()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();

        var actionGroup = AgvActionGenerator.GenerateIncompleteActionGroupWithoutDropTarget();
        var moveAction = actionGroup.Actions.Skip(1).FirstOrDefault();
        (string[] nodes, Guid[] edges) route = AgvGraphGenerator.CreateRoute(moveAction!.Target.NodeId!);
        moveAction.Nodes = route.nodes;
        moveAction.Edges = route.edges;

        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentState: nameof(SimulatedAgvStateMachine.Idle),
            currentAction:
            moveAction,
            agvGraph:
            graph);
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture(inactivityTimeout: TimeSpan.FromSeconds(6))
            .WithAgv(agvSaga)
            .Build();

        // Move action will be updated to Drop action. 
        var moveActionToDropAction = actionGroup.Actions.LastOrDefault();
        if (moveActionToDropAction != null)
        {
            (ICollection<string> dropNodes, ICollection<Guid> dropEdges) dropRoute = AgvGraphGenerator.CreateRoute(AgvGraphGenerator.DestinationFar.Id);
            var updatedMoveAction = new AgvAction(moveAction.CorrelationId, moveActionToDropAction.ActionType, AgvGraphGenerator.DestinationFar.Id, moveActionToDropAction.ParentId, moveActionToDropAction.InfoToAgv)
            {
                // Skip on node to simulate running AGV
                Nodes = dropRoute.dropNodes.Skip(1).ToArray(),
                Edges = dropRoute.dropEdges.Skip(1).ToArray()
            };

            // Start move action 
            await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, ActionStarted>(
                new StartActionCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, moveAction));

            //wait until saga is updated (move completed)
            await fixture.WaitUntil(agvSaga, s =>
                {
                    bool ok = !s.AgvStatus.Busy;
                    ok &= s.LastNodeId == moveAction.Target.NodeId;
                    return ok;
                },
                s =>
                {
                    s.AgvStatus.Busy.ShouldBeFalse();
                    s.LastNodeId.ShouldBe(moveAction.Target.NodeId);
                });

            // Update move action to drop action with new target
            var actionUpdated = await fixture.PublishAndGetFirstMessageAsync<UpdateActionCommand, ActionUpdated>(
                new UpdateActionCommand(correlationId, agvSaga.SerialNumber, updatedMoveAction));
            actionUpdated.CorrelationId.ShouldBe(updatedMoveAction.CorrelationId);

            // Position sent (Node travel time is twice as long as position sent interval, so this is surely sent in the meanwhile)
            (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();

            // action finished
            (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<UpdateActionCommand>()).ShouldBeTrue();
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
        var action = AgvActionGenerator.GenerateAction(startNodeId: node.Id, targetNodeId: node.Id, actionType: ActionType.Pick);
        action.Nodes = [node.Id];
        var agvLoadStatusReceived = await fixture.PublishAndGetFirstMessageAsync<StartActionCommand, AgvLoadStatusReceived>(
            new StartActionCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, action));

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
        var action = AgvActionGenerator.GenerateAction(startNodeId: node.Id, targetNodeId: node.Id, actionType: ActionType.Drop);
        action.Nodes = [node.Id];

        await fixture.PublishMessageAsync(new StartActionCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, action));
        await fixture.WaitUntilAgvIdle(agvSaga);
        var agvLoadStatusReceived = await fixture.GetFirstPublishedMessageAsync<AgvLoadStatusReceived>();
        agvLoadStatusReceived.Cargos.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task Consume_ActionAborted_InStatusBusyMoving_TransitionsToIdlePublishesActionAbortedEvent()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var action = AgvActionGenerator.GenerateAction(startNodeId: graph.Nodes.First().Id, targetNodeId: graph.Nodes.Skip(1).First().Id);
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

        await fixture.ShouldHavePublishedAsync<ActionAbortionCompleted>();
        agvSaga.ActionStartDate.ShouldBeNull();
        agvSaga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        agvSaga.LastActionAborted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ScheduleFinishAction_AfterActionWasAborted_ResetLastActionAborted()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var action = AgvActionGenerator.GenerateAction(startNodeId: graph.Nodes.First().Id, targetNodeId: graph.Nodes.Skip(1).First().Id);
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
        await fixture.ShouldHavePublishedAsync<ActionAbortionCompleted>();
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
            new SimulateAgvError(correlationId, new List<AgvError> {agvError}));

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

        await fixture.PublishMessageAsync(new SimulateAgvError(correlationId, new List<AgvError> {agvError}));

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

        await fixture.PublishMessageAsync(new SimulateAgvError(correlationId, new List<AgvError> {agvError}));
        await fixture.PublishMessageAsync(new ResetAgvErrors(correlationId, []));

        agvTravelSimulatorMock.Mock.Verify(m => m.Resume(), Times.Once);
    }
}