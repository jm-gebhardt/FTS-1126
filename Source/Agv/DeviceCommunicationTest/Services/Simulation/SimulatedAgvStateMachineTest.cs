using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
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
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation
{
    [TestClass]
    public class SimulatedAgvStateMachineTest
    {
        private const int InitialBatteryCharge = 75;

        [TestMethod]
        public async Task InitialCreation_SingleAgv_Successful()
        {
            var options = new SimulateOptions()
            {
                Agvs = 1,
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" },
                NodeTravelTime = 1000
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options)
            );
            const string agvId = "Agv#1";
            var agvCorrelationId = agvId.ToGuid();
            var graph = new GridGraphGenerator(new[] { "S1" }, new[] { "D1" }).Generate(agvCorrelationId);
            var data = new AgvData(agvCorrelationId, "model", "type", null, 10, 1, 1, 1, 1, 1, 1, 1, Array.Empty<AgvData.Sensor>());
            var map = new MapGenerator(graph);
            var agv = new SimulatedAgv(agvCorrelationId, agvId, "", graph, data, map.Generate());

            // Publish CreateAgv ...
            await fixture.TestHarness.Bus.Publish(new SimulatedAgvStateMachine.CreateAgv(agvCorrelationId, agv, options.NodeTravelTime));
            // ... wait for being consumed ...
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>()).ShouldBeTrue();
            // ... pick newly created saga.
            var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == agvCorrelationId).First().Saga;

            createdSaga.CorrelationId.ShouldBe(agvCorrelationId);
            createdSaga.AgvId.ShouldBe(agvId);
            createdSaga.AgvStatus.ShouldNotBeNull();
            createdSaga.AgvGraph.ShouldNotBeNull();
            createdSaga.AgvData.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task InitialCreation_SingleAgv_Status_Correct()
        {
            var options = new SimulateOptions()
            {
                Agvs = 1,
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" },
                NodeTravelTime = 1000
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options),
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler()
            );
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
            const string mapName = "Map#1";
            var graph = new AgvGraph(correlationId,
                new[]
                {
                    new AgvGraph.Node("S1") { Enabled = true, Type = AgvGraph.NodeType.Pick, X = 0f, Y = 0f, Z = 0f }
                },
                Array.Empty<AgvGraph.Edge>(),
                mapName.ToGuid(),
                mapName
            );
            var agv = new SimulatedAgv(correlationId, agvId, "", graph, null, null);

            // Publish CreateAgv ...
            await fixture.TestHarness.Bus.Publish(new SimulatedAgvStateMachine.CreateAgv(correlationId, agv, options.NodeTravelTime));
            // ... wait for being consumed ...
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>()).ShouldBeTrue();
            // ... pick newly created saga.
            var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == correlationId).First().Saga;

            var agvStatus = createdSaga.AgvStatus;
            agvStatus.ShouldNotBeNull();
            agvStatus.CorrelationId.ShouldBe(correlationId);
            agvStatus.OrderId.ShouldBeNull();
            agvStatus.Busy.ShouldBeFalse();
            agvStatus.Automatic.ShouldBeTrue();
            agvStatus.Charging.ShouldBeFalse();
            agvStatus.Disturbed.ShouldBeFalse();
            agvStatus.LastNodeId.ShouldBeNull();
        }

        [TestMethod]
        public async Task When_RequestAgvGraph_AgvGraphReceived_isPublished()
        {
            var options = new SimulateOptions()
            {
                Agvs = 1,
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" }
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options)
            );
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
            var graph = new GridGraphGenerator(new[] { "S1" }, new[] { "D1" }).Generate(correlationId);

            // Create saga in idle state with graph
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle), agvGraph: graph);
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
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" }
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options)
            );
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();

            // Create saga in idle state without graph
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle));
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
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" }
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options)
            );
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();

            // Create saga in idle state without agv data
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle));
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
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" }
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options)
            );
            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();
            var data = new AgvData(correlationId, "model", "type", null, 10, 1, 1, 1, 1, 1, 1, 1, Array.Empty<AgvData.Sensor>());

            // Create saga in idle state with agv data
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle), agvData: data);
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
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" }
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options)
            );
            const string agvId = "Agv#1";
            const string mapId = "Map#1";
            var correlationId = agvId.ToGuid();
            var graph = new GridGraphGenerator(new[] { "S1" }, new[] { "D1" }).Generate(correlationId);
            var map = new MapGenerator(graph).Generate();

            // Create saga in idle state with agv map
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle), agvGraph: graph, agvMap: map);
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
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" }
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options)
            );
            const string agvId = "Agv#1";
            const string mapId = "Map#1";
            var correlationId = agvId.ToGuid();

            // Create saga in idle state without agv map
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle));
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
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" }
            };
            // ReSharper disable once UseObjectOrCollectionInitializer
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options),
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler()
            );
            // Reduce interval for position messages.
            fixture.Machine.SignalPositionIntervalMs = 50;

            const string agvId = "Agv#1";
            var agv = agvId.ToGuid();
            const string orderId = "Order#1";
            var orderCorrelationId = orderId.ToGuid();
            var actionId = Guid.NewGuid();
            string source = "S1";
            string destination = "D1";
            /*   S1 (5.0/2.5)
             *    |
             *   WP (5.0/5.0)
             *    |
             *   D1 (5.0/7,5)   */
            var graph = new GridGraphGenerator(new[] { source }, new[] { destination }, gridRows: 1).Generate(agv);
            //   S1 -> WP -> D1
            string[] path =
            {
                source,
                graph.Nodes.Single(n => n.Type == AgvGraph.NodeType.WayPoint).Id,
                destination
            };
            var data = new AgvData(agv, "model", "type", null, 10, 1, 1, 1, 1, 1, 1, 1, Array.Empty<AgvData.Sensor>());
            // Travel duration per edge will be set about twice of the position message interval.
            int nodeTravelTime = (int)(2f * fixture.Machine.SignalPositionIntervalMs);

            // Create saga in idle state
            var agvSaga = new SimulatedAgvSaga(agv, agvId, nodeTravelTime, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle), agvGraph: graph, agvData: data);
            fixture.Add(agvSaga);

            agvSaga.AgvStatus.Busy.ShouldBeFalse();
            agvSaga.LastNodeId.ShouldBeNull();
            agvSaga.NextNodeId.ShouldBeNull();

            // Start action: Publish StartActionCommand ...
            await fixture.TestHarness.Bus.Publish(new StartActionCommand(agv,
                new AgvAction(actionId, agvId, ActionType.Drop, orderId, orderCorrelationId, source, destination)
                {
                    Nodes = path
                }
            ));

            // ... wait for being consumed ...
            (await fixture.SagaHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();
            // ... wait for action started is being sent (by state machine) ...
            (await fixture.TestHarness.Published.Any<ActionStatusChangedEvent>()).ShouldBeTrue();
            var actionStatus = fixture.TestHarness.Published.Select<ActionStatusChangedEvent>()
                .First(c => c.Context.Message.ChangedType == ActionStatusChangedType.Started)
                .Context.Message;
            actionStatus.CorrelationId.ShouldBe(actionId);

            agvSaga.AgvStatus.Busy.ShouldBeTrue();
            agvSaga.TimeToFinish.ShouldNotBeNull();
            agvSaga.ActionStartDate.ShouldNotBeNull();
            agvSaga.ActionFinishDate.ShouldNotBeNull();

            // Position sent (Node travel time is twice as long as position sent interval, so this is surely sent in the meanwhile)
            (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleSignalPosition>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleSignalPosition>()).ShouldBeTrue();

            // Order completed
            (await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishAction>()).ShouldBeTrue();

            //wait until saga is updated
            var startedTime = DateTime.Now;
            var timeout = TimeSpan.FromMilliseconds(2500);
            bool matches;
            do
            {
                matches = !agvSaga.AgvStatus.Busy;
                matches = matches && agvSaga.TimeToFinish == null;
                matches = matches && agvSaga.ActionStartDate == null;
                matches = matches && agvSaga.ActionFinishDate == null;
                matches = matches && agvSaga.LastNodeId == destination;
                matches = matches && agvSaga.NextNodeId == destination;
            } while (!matches && DateTime.Now < startedTime + timeout);
            matches.ShouldBeTrue();

            (await fixture.TestHarness.Published.Any<ActionStatusChangedEvent>()).ShouldBeTrue();
            actionStatus = fixture.TestHarness.Published.Select<ActionStatusChangedEvent>()
                .First(c => c.Context.Message.ChangedType == ActionStatusChangedType.Completed)
                .Context.Message;
            actionStatus.CorrelationId.ShouldBe(actionId);

            // There should be at least 2 positions sent (one or two per edge and we have two edges)
            int count = fixture.SagaHarness.Consumed.Select<SimulatedAgvStateMachine.ScheduleSignalPosition>().Count();
            count.ShouldBeGreaterThanOrEqualTo(2);

            // Last position message received should be the position of the last node
            var positionMessage = fixture.TestHarness.Published.Select<AgvPositionReceived>().Last().Context.Message;
            var destinationNode = graph.Nodes.First(n => n.Id == destination);
            positionMessage.AgvPosition.X.ShouldBe(destinationNode.X, 0.00001f);
            positionMessage.AgvPosition.Y.ShouldBe(destinationNode.Y, 0.00001f);
            positionMessage.AgvPosition.Z.ShouldBe(destinationNode.Z, 0.00001f);
        }

        [TestMethod]
        public async Task When_AGV_Alive_Publish_AgvStatusReceived()
        {
            var options = new SimulateOptions()
            {
                Agvs = 1,
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" },
                NodeTravelTime = 500
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options),
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler()
            );

            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();

            // ReSharper disable once UseObjectOrCollectionInitializer
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 500, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle));
            agvSaga.SimulatedBatteryCharge = 100; // Set charge to 100 for alive and 0 for not alive
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
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" },
                NodeTravelTime = 500
            };
            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services => services.AddSingleton(options),
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler()
            );

            const string agvId = "Agv#1";
            var correlationId = agvId.ToGuid();

            // ReSharper disable once UseObjectOrCollectionInitializer
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 500, InitialBatteryCharge, currentState: nameof(SimulatedAgvStateMachine.Idle));
            agvSaga.SimulatedBatteryCharge = 0; // Set charge to 100 for alive and 0 for not alive
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
    }
}