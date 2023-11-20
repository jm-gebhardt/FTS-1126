using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Messages;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

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
        string agvId = agvSaga.AgvId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        // Publish RequestAgvGraph ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvId));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>()).ShouldBeTrue();
        // ... expect AgvGraphReceived being published.
        (await fixture.TestHarness.Published.Any<AgvGraphReceived>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.AgvId.ShouldBe(agvId);
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
        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvSaga.AgvId));
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
        await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvSaga.AgvId));
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
        string agvId = agvSaga.AgvId;
        await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvId));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvData>()).ShouldBeTrue();
        // ... expect AgvDataReceived being published.
        (await fixture.TestHarness.Published.Any<AgvDataReceived>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<AgvDataReceived>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.AgvId.ShouldBe(agvId);
        message.Data.ShouldBe(agvSaga.AgvData);
    }

    [TestMethod]
    public async Task When_RequestAgvMap_AgvMapReceived_isPublished()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph
        );
        var correlationId = agvSaga.CorrelationId;
        string mapId = agvSaga.AgvMap!.MapId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        // Publish RequestAgvMap ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvMap(correlationId, agvSaga.AgvId, mapId.ToGuid(), mapId));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvMap>()).ShouldBeTrue();
        // ... expect AgvMapReceived being published.
        (await fixture.TestHarness.Published.Any<AgvMapReceived>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<AgvMapReceived>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.AgvId.ShouldBe(agvSaga.AgvId);
        message.AgvMap.ImageData.ShouldBe(agvSaga.AgvMap!.ImageData);
    }

    [TestMethod]
    public async Task When_RequestAgvMap_AgvMapIsNull_NoMessageIsPublished()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        var node = graph.Nodes.First();
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            currentNode: node,
            agvGraph: graph,
            mapExists: false
        );
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        // Publish RequestAgvMap ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvMap(correlationId, agvSaga.AgvId, Guid.NewGuid(), "non existing map"));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvMap>()).ShouldBeTrue();
        // ... but don't expect AgvMapReceived being published.
        var token = new CancellationTokenSource();
        token.CancelAfter(100);
        (await fixture.TestHarness.Published.Any<AgvMapReceived>(token.Token)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task When_StartActionCommand_ActionStarted_IsPublished_And_ActionCompleted_Is_Published()
    {
        const int signalPositionIntervalMs = 50;
        // Travel duration per edge will be set about twice of the position message interval.
        const int nodeTravelTime = (int) (2f * signalPositionIntervalMs);

        var graph = AgvGraphGenerator.CreateDefaultGraph();
        ICollection<GraphNode> nodes = graph.Nodes;
        var startNode = nodes.First();
        string destination = nodes.Skip(1).First().Id;
        var action = AgvActionGenerator.GenerateMoveAction(startNodeId: startNode.Id, targetNodeId: destination, availableNodes: nodes, numberOfNodes: 3);
        var agvSaga = SimulatedAgvSagaGenerator.Generate(nodeTravelTime: nodeTravelTime, agvGraph: graph, currentNode: startNode);
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithSignalPositionIntervalMs(signalPositionIntervalMs)
            .WithAgv(agvSaga)
            .Build();

        await fixture.PublishMessageAsync(new StartActionCommand(agvSaga.CorrelationId, agvSaga.AgvId, action));

        var actionStarted = await fixture.GetFirstPublishedMessageAsync<ActionStarted>();

        var actionId = action.CorrelationId;
        actionStarted.CorrelationId.ShouldBe(actionId);

        agvSaga.AgvStatus.Busy.ShouldBeTrue();
        agvSaga.TimeToFinish.ShouldNotBeNull();
        agvSaga.ActionStartDate.ShouldNotBeNull();
        agvSaga.ActionFinishDate.ShouldNotBeNull();

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
                ok &= s.TimeToFinish == null;
                ok &= s.ActionStartDate == null;
                ok &= s.ActionFinishDate == null;
                ok &= s.LastNodeId == destination;
                ok &= s.NextNodeId == destination;
                return ok;
            },
            s =>
            {
                s.AgvStatus.Busy.ShouldBeFalse();
                s.TimeToFinish.ShouldBeNull();
                s.ActionStartDate.ShouldBeNull();
                s.ActionFinishDate.ShouldBeNull();
                s.LastNodeId.ShouldBe(destination);
                s.NextNodeId.ShouldBe(destination);
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

        await fixture.PublishMessageAsync(new StartActionCommand(agvSaga.CorrelationId, agvSaga.AgvId, action));

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

        await fixture.PublishMessageAsync(new StartActionCommand(agvSaga.CorrelationId, agvSaga.AgvId, action));

        await fixture.WaitUntilAgvIdle(agvSaga);
        var agvLoadStatusReceived = await fixture.GetFirstPublishedMessageAsync<AgvLoadStatusReceived>();
        agvLoadStatusReceived.Cargos.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task When_AGV_Alive_Publish_AgvStatusReceived()
    {
        var agvSaga = SimulatedAgvSagaGenerator.Generate();
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        // Trigger status update scheduler
        await fixture.PublishMessageAsync(new RequestStatusSendSchedulerStart(correlationId, agvSaga.AgvId), c => c.Context.Message.CorrelationId == correlationId);

        //Check if Scheduler sends status update
        (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleStatusUpdate>(c => c.Context.Message.CorrelationId == correlationId)).ShouldBeTrue();

        //Check if status update gets consumed
        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleStatusUpdate>(c => c.Context.Message.CorrelationId == correlationId)).ShouldBeTrue();

        var token = new CancellationTokenSource();
        token.CancelAfter(400);

        //Check if AgvStatusIsReceived was sent. Only true if agv is alive.
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>(c => c.Context.Message.CorrelationId == correlationId, token.Token)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task When_AGV_Dead_NoPublish_AgvStatusReceived()
    {
        var agvSaga = SimulatedAgvSagaGenerator.Generate(initialSoc: 0);
        var correlationId = agvSaga.CorrelationId;
        var fixture = new SimulatedAgvStateMachineTestFixture()
            .WithAgv(agvSaga)
            .Build();

        // Trigger status update scheduler
        await fixture.TestHarness.Bus.Publish(new RequestStatusSendSchedulerStart(correlationId, agvSaga.AgvId));
        (await fixture.SagaHarness.Consumed.Any<RequestStatusSendSchedulerStart>(c => c.Context.Message.CorrelationId == correlationId)).ShouldBeTrue();

        //Check if Scheduler sends status update
        (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleStatusUpdate>(c => c.Context.Message.CorrelationId == correlationId)).ShouldBeTrue();

        //Check if status update gets consumed
        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleStatusUpdate>(c => c.Context.Message.CorrelationId == correlationId)).ShouldBeTrue();

        var token = new CancellationTokenSource();
        token.CancelAfter(400);

        //Check if AgvStatusIsReceived was sent. Only true if agv is alive.
        (await fixture.TestHarness.Published.Any<AgvStatusReceived>(c => c.Context.Message.CorrelationId == correlationId, token.Token)).ShouldBeFalse();
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

        await fixture.PublishMessageAsync(new ActionAborted(correlationId, action), c => c.Context.Message.CorrelationId == correlationId);

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

        await fixture.PublishMessageAsync(new ActionAborted(correlationId, action), c => c.Context.Message.CorrelationId == correlationId);
        (await fixture.TestHarness.Published.Any<ActionAbortionCompleted>()).ShouldBeTrue();
        agvSaga.ActionStartDate.ShouldBeNull();
        agvSaga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        agvSaga.LastActionAborted.ShouldBeTrue();

        await fixture.PublishMessageAsync(new SimulatedAgvStateMachine.ScheduleFinishAction(correlationId));
    }
}