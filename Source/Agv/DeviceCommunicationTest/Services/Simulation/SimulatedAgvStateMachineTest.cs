using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Services.Simulation;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation.Travel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        // Publish RequestAgvGraph ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvSerialNumber));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();
        // ... expect AgvGraphReceived being published.
        (await fixture.TestHarness.Published.Any<AgvGraphReceived>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;
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
        (await fixture.TestHarness.Published.Any<AgvGraphReceived>(token.Token)).ShouldBeFalse();
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

        // Publish RequestAgvData ...
        string agvSerialNumber = agvSaga.SerialNumber;
        await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvSerialNumber));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvData>()).ShouldBeTrue();
        // ... expect AgvDataReceived being published.
        (await fixture.TestHarness.Published.Any<AgvDataReceived>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<AgvDataReceived>().First().Context.Message;
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
        action.Nodes = GetArbitraryRoute(action.TargetNodeId, graph);
        var startNode = nodes.First(n => n.Id == action.Nodes.First());
        string destination = action.Nodes.Last();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(simulationSpeed: 2, agvGraph: graph, currentNode: startNode);
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        await fixture.PublishMessageAsync(new StartActionCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, action));

        var actionStarted = await fixture.GetFirstPublishedMessageAsync<ActionStarted>();

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

        (await fixture.TestHarness.Published.Any<ActionCompleted>()).ShouldBeTrue();
        var actionCompleted = fixture.TestHarness.Published.Select<ActionCompleted>().First().Context.Message;
        actionCompleted.CorrelationId.ShouldBe(actionId);

        // Last position message received should be the position of the last node
        var destinationNode = nodes.First(n => n.Id == destination);
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.X - destinationNode.X) < 0.01)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Y - destinationNode.Y) < 0.01)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Z - destinationNode.Z) < 0.01)).ShouldBeTrue();
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
        var action = AgvActionGenerator.GenerateAction(startNodeId: node.Id, targetNodeId: node.Id, actionType: ActionType.Pick);

        await fixture.PublishMessageAsync(new StartActionCommand(agvSaga.CorrelationId, agvSaga.SerialNumber, action));

        await fixture.WaitUntilAgvIdle(agvSaga);
        var agvLoadStatusReceived = await fixture.GetFirstPublishedMessageAsync<AgvLoadStatusReceived>();
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
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();
        var action = AgvActionGenerator.GenerateAction(startNodeId: node.Id, targetNodeId: node.Id, actionType: ActionType.Drop);

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

        (await fixture.TestHarness.Published.Any<ActionAbortionCompleted>()).ShouldBeTrue();
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
        (await fixture.TestHarness.Published.Any<ActionAbortionCompleted>()).ShouldBeTrue();
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

        await fixture.PublishMessageAsync(new SimulateAgvError(correlationId, new List<AgvError> {agvError}));

        (await fixture.SagaHarness.Consumed.Any<SimulateAgvError>()).ShouldBeTrue();

        (await fixture.TestHarness.Published.Any<AgvErrorStatusReceived>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<AgvErrorStatusReceived>().First().Context.Message;
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
    public async Task Consume_SimulateAgvError_CallsTravelSimulatorResume_WhenErrorRemoved()
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
        await fixture.PublishMessageAsync(new SimulateAgvError(correlationId, new List<AgvError>()), m => (m.MessageObject as SimulateAgvError)?.AgvErrors.Count == 0);

        agvTravelSimulatorMock.Mock.Verify(m => m.Resume(), Times.Once);
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

    private string[] GetArbitraryRoute(string targetNode, AgvGraph graph)
    {
        var nodes = new List<string> {targetNode};
        foreach (int unused in Enumerable.Range(0, 3))
        {
            var edge = graph.Edges.FirstOrDefault(e => e.ToNodeId == targetNode && !nodes.Contains(e.FromNodeId));
            if (edge == null)
            {
                break;
            }
            nodes.Add(edge.FromNodeId);
            targetNode = edge.FromNodeId;
        }
        nodes.Reverse();
        return nodes.ToArray();
    }
}