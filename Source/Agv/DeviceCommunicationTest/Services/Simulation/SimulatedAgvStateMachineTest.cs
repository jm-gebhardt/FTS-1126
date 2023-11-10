using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Messages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class SimulatedAgvStateMachineTest
{
    [TestMethod]
    public async Task When_RequestAgvGraph_AgvGraphReceived_isPublished()
    {
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options)
        );
        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();
        var graph = new GridGraphGenerator(new[] {"S1"}, new[] {"D1"}).Generate(correlationId);

        // Create saga in idle state with graph
        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            agvGraph: graph);
        fixture.Add(agvSaga);

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
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options)
        );
        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();

        // Create saga in idle state without graph
        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            graphExists: false);
        fixture.Add(agvSaga);

        // Publish RequestAgvGraph ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvId));
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
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options)
        );
        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();

        // Create saga in idle state without agv data
        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            dataExists: false);
        fixture.Add(agvSaga);

        // Publish RequestAgvData ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvId));
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
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options)
        );
        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();
        var data = new AgvData(correlationId, "model", "type", null, 10, 1, 1, 1, 1, 1, 1, 1, Array.Empty<AgvData.Sensor>());

        // Create saga in idle state with agv data
        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            agvData: data);
        fixture.Add(agvSaga);

        // Publish RequestAgvData ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvId));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvData>()).ShouldBeTrue();
        // ... expect AgvDataReceived being published.
        (await fixture.TestHarness.Published.Any<AgvDataReceived>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<AgvDataReceived>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.AgvId.ShouldBe(agvId);
        message.Data.ShouldBe(data);
    }

    [TestMethod]
    public async Task When_RequestAgvMap_AgvMapReceived_isPublished()
    {
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options),
            inactivityTimeout: TimeSpan.FromSeconds(6)
        );
        const string agvId = "Agv#1";
        const string mapId = "Map#1";
        var correlationId = agvId.ToGuid();
        var graph = new GridGraphGenerator(new[] {"S1"}, new[] {"D1"}).Generate(correlationId);
        var map = new MapGenerator(graph).Generate();

        // Create saga in idle state with agv map
        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            agvMap: map);
        fixture.Add(agvSaga);

        // Publish RequestAgvMap ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvMap(correlationId, agvId, mapId.ToGuid(), mapId));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvMap>()).ShouldBeTrue();
        // ... expect AgvMapReceived being published.
        (await fixture.TestHarness.Published.Any<AgvMapReceived>()).ShouldBeTrue();

        var message = fixture.TestHarness.Published.Select<AgvMapReceived>().First().Context.Message;
        message.CorrelationId.ShouldBe(correlationId);
        message.AgvId.ShouldBe(agvId);
        message.AgvMap.ImageData.ShouldBe(agvSaga.AgvMap!.ImageData);
    }

    [TestMethod]
    public async Task When_RequestAgvMap_AgvMapIsNull_NoMessageIsPublished()
    {
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options)
        );
        const string agvId = "Agv#1";
        const string mapId = "Map#1";
        var correlationId = agvId.ToGuid();

        // Create saga in idle state without agv map
        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            mapExists: false);
        fixture.Add(agvSaga);

        // Publish RequestAgvMap ...
        await fixture.TestHarness.Bus.Publish(new RequestAgvMap(correlationId, agvId, mapId.ToGuid(), mapId));
        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<RequestAgvMap>()).ShouldBeTrue();
        // ... but don't expect AgvMapReceived being published.
        var token = new CancellationTokenSource();
        token.CancelAfter(100);
        (await fixture.TestHarness.Published.Any<AgvMapReceived>(token.Token)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task When_ExecuteTransportOrder_TransportOrderStarted_IsPublished_TransportOrderCompleted_Is_Published()
    {
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        // ReSharper disable once UseObjectOrCollectionInitializer
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options),
            x => x.AddDelayedMessageScheduler(),
            (_, cfg) => cfg.UseDelayedMessageScheduler()
        );
        // Reduce interval for position messages.
        fixture.Machine.SignalPositionIntervalMs = 50;
        fixture.Machine.StatusUpdateIntervalMs = 50;

        const string agvId = "Agv#1";
        var agv = agvId.ToGuid();
        var actionId = Guid.NewGuid();
        var actionGroupId = Guid.Empty;
        const string source = "S1";
        const string destination = "D1";
        /*   S1 (5.0/2.5)
         *    |
         *   WP (5.0/5.0)
         *    |
         *   D1 (5.0/7,5)   */
        var graph = new GridGraphGenerator(new[] {source}, new[] {destination}, 1).Generate(agv);
        //   S1 -> WP -> D1
        string[] path =
        {
            source,
            graph.Nodes.Single(n => n.Type == AgvGraph.NodeType.WayPoint).Id,
            destination
        };
        // Travel duration per edge will be set about twice of the position message interval.
        int nodeTravelTime = (int) (2f * fixture.Machine.SignalPositionIntervalMs);

        // Create saga in idle state
        var agvSaga = SimulatedAgvSagaGenerator.Generate(
            agvId: agvId,
            currentNode: graph.Nodes.First(n => n.Id == source),
            agvGraph: graph,
            nodeTravelTime: nodeTravelTime
        );
        fixture.Add(agvSaga);

        agvSaga.AgvStatus.Busy.ShouldBeFalse();
        // randomized start node
        agvSaga.LastNodeId.ShouldNotBeNull();
        agvSaga.NextNodeId.ShouldBe(agvSaga.LastNodeId);

        // Start action: Publish StartActionCommand ...
        await fixture.TestHarness.Bus.Publish(new StartActionCommand(agv, agvId,
            new AgvAction(actionId, ActionType.Drop, destination, actionGroupId)
            {
                Nodes = path
            }));

        // ... wait for being consumed ...
        (await fixture.SagaHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();
        // ... wait for action started is being sent (by state machine) ...
        (await fixture.TestHarness.Published.Any<ActionStarted>()).ShouldBeTrue();
        var actionStarted = fixture.TestHarness.Published.Select<ActionStarted>().First().Context.Message;
        actionStarted.CorrelationId.ShouldBe(actionId);

        agvSaga.AgvStatus.Busy.ShouldBeTrue();
        agvSaga.TimeToFinish.ShouldNotBeNull();
        agvSaga.ActionStartDate.ShouldNotBeNull();
        agvSaga.ActionFinishDate.ShouldNotBeNull();

        // Position sent (Node travel time is twice as long as position sent interval, so this is surely sent in the meanwhile)
        (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.SchedulePositionUpdate>()).ShouldBeTrue();

        // Order completed
        (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();

        //wait until saga is updated
        var startedTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(5000);
        bool matches;
        do
        {
            matches = !agvSaga.AgvStatus.Busy;
            matches = matches && agvSaga.TimeToFinish == null;
            matches = matches && agvSaga.ActionStartDate == null;
            matches = matches && agvSaga.ActionFinishDate == null;
            matches = matches && agvSaga.LastNodeId == destination;
            matches = matches && agvSaga.NextNodeId == destination;
        } while (!matches && DateTime.UtcNow < startedTime + timeout);
        agvSaga.AgvStatus.Busy.ShouldBeFalse();
        agvSaga.TimeToFinish.ShouldBeNull();
        agvSaga.ActionStartDate.ShouldBeNull();
        agvSaga.ActionFinishDate.ShouldBeNull();
        agvSaga.LastNodeId.ShouldBe(destination);
        agvSaga.NextNodeId.ShouldBe(destination);

        (await fixture.TestHarness.Published.Any<ActionCompleted>()).ShouldBeTrue();
        var actionCompleted = fixture.TestHarness.Published.Select<ActionCompleted>().First().Context.Message;
        actionCompleted.CorrelationId.ShouldBe(actionId);

        // Last position message received should be the position of the last node
        var destinationNode = graph.Nodes.First(n => n.Id == destination);
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.X - destinationNode.X) < 0.01)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Y - destinationNode.Y) < 0.01)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvPositionReceived>(c => Math.Abs(c.Context.Message.AgvPosition.Z - destinationNode.Z) < 0.01)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task When_AGV_Alive_Publish_AgvStatusReceived()
    {
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"},
            NodeTravelTime = 500
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options),
            x => x.AddDelayedMessageScheduler(),
            (_, cfg) => cfg.UseDelayedMessageScheduler()
        );

        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();

        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId);
        fixture.Add(agvSaga);

        // Trigger status update scheduler
        await fixture.TestHarness.Bus.Publish(new RequestStatusSendSchedulerStart(correlationId, agvId));
        (await fixture.SagaHarness.Consumed.Any<RequestStatusSendSchedulerStart>(c => c.Context.Message.CorrelationId == correlationId)).ShouldBeTrue();

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
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"},
            NodeTravelTime = 500
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options),
            x => x.AddDelayedMessageScheduler(),
            (_, cfg) => cfg.UseDelayedMessageScheduler()
        );

        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();

        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            initialSoc: 0);
        fixture.Add(agvSaga);

        // Trigger status update scheduler
        await fixture.TestHarness.Bus.Publish(new RequestStatusSendSchedulerStart(correlationId, agvId));
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
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options),
            inactivityTimeout: TimeSpan.FromSeconds(6)
        );
        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();
        var graph = new GridGraphGenerator(new[] {"S1"}, new[] {"D1"}).Generate(correlationId);
        var map = new MapGenerator(graph).Generate();
        var action = new AgvAction(Guid.NewGuid(), ActionType.Drop, "D1", Guid.NewGuid());
        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            currentState: nameof(SimulatedAgvStateMachine.BusyMoving),
            currentAction:
            action,
            agvGraph:
            graph, agvMap:
            map);
        fixture.Add(agvSaga);
        await fixture.TestHarness.Bus.Publish(new ActionAborted(correlationId, action));
        (await fixture.SagaHarness.Consumed.Any<ActionAborted>(c => c.Context.Message.CorrelationId == correlationId)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionAbortionCompleted>()).ShouldBeTrue();
        agvSaga.ActionStartDate.ShouldBeNull();
        agvSaga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        agvSaga.LastActionAborted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ScheduleFinishAction_AfterActionWasAborted_ResetLastActionAborted()
    {
        var options = new SimulateOptions()
        {
            Agvs = 1,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"}
        };
        var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
            services => services.AddSingleton(options),
            inactivityTimeout: TimeSpan.FromSeconds(6)
        );
        const string agvId = "Agv#1";
        var correlationId = agvId.ToGuid();
        var graph = new GridGraphGenerator(new[] {"S1"}, new[] {"D1"}).Generate(correlationId);
        var map = new MapGenerator(graph).Generate();
        var action = new AgvAction(Guid.NewGuid(), ActionType.Drop, "D1", Guid.NewGuid());
        var agvSaga = SimulatedAgvSagaGenerator.Generate(agvId: agvId,
            currentState: nameof(SimulatedAgvStateMachine.BusyMoving),
            currentAction: action,
            agvGraph: graph, agvMap: map);
        fixture.Add(agvSaga);
        await fixture.TestHarness.Bus.Publish(new ActionAborted(correlationId, action));
        (await fixture.SagaHarness.Consumed.Any<ActionAborted>(c => c.Context.Message.CorrelationId == correlationId)).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<ActionAbortionCompleted>()).ShouldBeTrue();
        agvSaga.ActionStartDate.ShouldBeNull();
        agvSaga.CurrentState.ShouldBe(nameof(SimulatedAgvStateMachine.Idle));
        agvSaga.LastActionAborted.ShouldBeTrue();

        await fixture.TestHarness.Bus.Publish(new SimulatedAgvStateMachine.ScheduleFinishAction(correlationId));
        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
    }
}