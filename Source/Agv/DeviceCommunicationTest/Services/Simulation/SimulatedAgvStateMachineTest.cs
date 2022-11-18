using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation
{
    [TestClass]
    public class SimulatedAgvStateMachineTest
    {
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
            var correlationId = agvId.ToGuid();
            var graph = new GridGraphGenerator(new[] { "S1" }, new[] { "D1" }).Generate(correlationId);
            var data = new AgvData(correlationId, "model", "type",null, 10, 1, 1, 1, 1 ,1, 1, 1, Array.Empty<AgvData.Sensor>());
            var map = new MapGenerator(graph);
            var agv = new SimulatedAgv(correlationId, agvId , "", graph, data, map.Generate());

            // Publish CreateAgv ...
            await fixture.TestHarness.Bus.Publish(new SimulatedAgvStateMachine.CreateAgv(correlationId, agv, options.NodeTravelTime));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>());
            // ... pick newly created saga.
            var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == correlationId).First().Saga;

            Assert.AreEqual(correlationId, createdSaga.CorrelationId);
            Assert.AreEqual(agvId, createdSaga.AgvId);
            Assert.IsNotNull(createdSaga.AgvStatus);
            Assert.IsNotNull(createdSaga.AgvGraph);
            Assert.IsNotNull(createdSaga.AgvData);
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
                    new AgvGraph.Node("S1") { Enabled = true, Type = AgvGraph.NodeType.Pick, X =  0f, Y = 0f, Z = 0f }
                },
                Array.Empty<AgvGraph.Edge>(), 
                mapName.ToGuid(),
                mapName
            );
            var agv = new SimulatedAgv(correlationId, agvId , "", graph, null, null);

            // Publish CreateAgv ...
            await fixture.TestHarness.Bus.Publish(new SimulatedAgvStateMachine.CreateAgv(correlationId, agv, options.NodeTravelTime));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>());
            // ... pick newly created saga.
            var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == correlationId).First().Saga;

            var agvStatus = createdSaga.AgvStatus;
            Assert.IsNotNull(agvStatus);
            Assert.AreEqual(correlationId, agvStatus.CorrelationId);
            Assert.IsNull(agvStatus.OrderId);
            Assert.IsFalse(agvStatus.Busy);
            Assert.IsFalse(agvStatus.Automatic);
            Assert.IsFalse(agvStatus.Charging);
            Assert.IsFalse(agvStatus.Disturbed);
            Assert.IsNull(agvStatus.LastNodeId);
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
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, currentState: nameof(SimulatedAgvStateMachine.Idle), agvGraph: graph);
            fixture.Add(agvSaga);

            // Publish RequestAgvGraph ...
            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvId));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());
            // ... expect AgvGraphReceived being published.
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvGraphReceived>());

            var message = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(agvId, message.AgvId);
            Assert.AreEqual(graph, message.Graph);
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
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, currentState: nameof(SimulatedAgvStateMachine.Idle));
            fixture.Add(agvSaga);

            // Publish RequestAgvGraph ...
            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, agvId));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());
            // ... but don't expect AgvGraphReceived being published.
            var token = new CancellationTokenSource();
            token.CancelAfter(100);
            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvGraphReceived>(token.Token));
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
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, currentState: nameof(SimulatedAgvStateMachine.Idle));
            fixture.Add(agvSaga);

            // Publish RequestAgvData ...
            await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvId));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvData>());
            // ... but don't expect AgvDataReceived being published.
            var token = new CancellationTokenSource();
            token.CancelAfter(100);
            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvDataReceived>(token.Token));
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
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, currentState: nameof(SimulatedAgvStateMachine.Idle), agvData: data);
            fixture.Add(agvSaga);

            // Publish RequestAgvData ...
            await fixture.TestHarness.Bus.Publish(new RequestAgvData(correlationId, agvId));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvData>());
            // ... expect AgvDataReceived being published.
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvDataReceived>());

            var message = fixture.TestHarness.Published.Select<AgvDataReceived>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(agvId, message.AgvId);
            Assert.AreEqual(data, message.Data);
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
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, currentState: nameof(SimulatedAgvStateMachine.Idle), agvGraph: graph, agvMap: map);
            fixture.Add(agvSaga);

            // Publish RequestAgvMap ...
            await fixture.TestHarness.Bus.Publish(new RequestAgvMap(correlationId, agvId, mapId.ToGuid(), mapId));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvMap>());
            // ... expect AgvMapReceived being published.
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvMapReceived>());

            var message = fixture.TestHarness.Published.Select<AgvMapReceived>().First().Context.Message;
            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(agvId, message.AgvId);
            Assert.AreEqual(agvSaga.AgvMap!.ImageData, message.AgvMap.ImageData);
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
            var agvSaga = new SimulatedAgvSaga(correlationId, agvId, 5000, currentState: nameof(SimulatedAgvStateMachine.Idle));
            fixture.Add(agvSaga);

            // Publish RequestAgvMap ...
            await fixture.TestHarness.Bus.Publish(new RequestAgvMap(correlationId, agvId, mapId.ToGuid(), mapId));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvMap>());
            // ... but don't expect AgvMapReceived being published.
            var token = new CancellationTokenSource();
            token.CancelAfter(100);
            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvMapReceived>(token.Token));
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
            var agvCorrelationId = agvId.ToGuid();
            const string orderId = "Order#1";
            var orderCorrelationId = orderId.ToGuid();
            var order = new OrderDetails(orderCorrelationId, orderId, "S1", "D1", "123", HostOrderCreator.Simulation);
            /*   S1 (5.0/2.5)
             *    |
             *   WP (5.0/5.0)
             *    |
             *   D1 (5.0/7,5)   */
            var graph = new GridGraphGenerator(new[] {order.Source}, new[] {order.Destination}, gridRows: 1).Generate(agvCorrelationId);
            //   S1 -> WP -> D1
            string[] path = {
                order.Source,
                graph.Nodes.Single(n => n.Type == AgvGraph.NodeType.WayPoint).Id,
                order.Destination
            };
            var data = new AgvData(agvCorrelationId, "model", "type", null, 10, 1, 1, 1, 1, 1, 1, 1, Array.Empty<AgvData.Sensor>());
            // Travel duration per edge will be set about twice of the position message interval.
            int nodeTravelTime = (int)(2f * fixture.Machine.SignalPositionIntervalMs);

            // Create saga in idle state
            var agvSaga = new SimulatedAgvSaga(agvCorrelationId, agvId, nodeTravelTime, currentState: nameof(SimulatedAgvStateMachine.Idle), agvGraph: graph, agvData: data);
            fixture.Add(agvSaga);

            Assert.IsFalse(agvSaga.AgvStatus!.Busy);
            Assert.IsNull(agvSaga.LastNodeId);
            Assert.IsNull(agvSaga.NextNodeId);

            // Start Order: Publish ExecuteTransportOrder ...
            await fixture.TestHarness.Bus.Publish(new ExecuteTransportOrder(agvCorrelationId, order, agvId, path));
            // ... wait for being consumed ...
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<ExecuteTransportOrder>());
            // ... wait for TransportOrderStarted is being sent (by state machine) ...
            Assert.IsTrue(await fixture.TestHarness.Published.Any<TransportOrderStarted>());
            var startedMessage = fixture.TestHarness.Published.Select<TransportOrderStarted>().First().Context.Message;
            Assert.AreEqual(orderCorrelationId, startedMessage.CorrelationId);

            Assert.IsTrue(agvSaga.AgvStatus!.Busy);
            Assert.IsNotNull(agvSaga.TimeToFinish);
            Assert.IsNotNull(agvSaga.OrderStartDate);
            Assert.IsNotNull(agvSaga.OrderFinishDate);

            // wait until order is picked up
            int delay = (int)(agvSaga.TimeToPick!.Value.TotalMilliseconds + 0.5);
            if (delay > 0)
            {
                Thread.Sleep(delay);
            }

            // Order picked up
            Assert.IsTrue(await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.SchedulePickOrder>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.SchedulePickOrder>());

            // Position sent (Node travel time is twice as long as position sent interval, so this is surely sent in the meanwhile)
            Assert.IsTrue(await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleSignalPosition>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleSignalPosition>());

            Assert.IsTrue(await fixture.TestHarness.Published.Any<TransportOrderPickCompleted>());
            var pickedMessage = fixture.TestHarness.Published.Select<TransportOrderPickCompleted>().First().Context.Message;
            Assert.AreEqual(order.CorrelationId, pickedMessage.CorrelationId);

            // wait until order is completed
            delay = (int)(agvSaga.TimeToFinish!.Value.TotalMilliseconds + 0.5);
            if (delay > 0)
            {
                Thread.Sleep(delay);
            }

            // Order completed
            Assert.IsTrue(await fixture.TestHarness.Sent.Any<SimulatedAgvStateMachine.ScheduleFinishOrder>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.ScheduleFinishOrder>());

            Assert.IsFalse(agvSaga.AgvStatus!.Busy);
            Assert.IsNull(agvSaga.TimeToFinish);
            Assert.IsNull(agvSaga.OrderStartDate);
            Assert.IsNull(agvSaga.OrderFinishDate);
            Assert.AreEqual(order.Destination, agvSaga.LastNodeId);
            Assert.AreEqual(order.Destination, agvSaga.NextNodeId);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<TransportOrderCompleted>());
            var completedMessage = fixture.TestHarness.Published.Select<TransportOrderCompleted>().First().Context.Message;
            Assert.AreEqual(order.CorrelationId, completedMessage.CorrelationId);

            // There should be at least 2 positions sent (one or two per edge and we have two edges)
            int count = fixture.SagaHarness.Consumed.Select<SimulatedAgvStateMachine.ScheduleSignalPosition>().Count();
            Assert.IsTrue(count >= 2);

            // When order is finished there shouldn't be any more position messages ...
            Thread.Sleep(fixture.Machine.SignalPositionIntervalMs);
            // ... even when we wait for the interval time.
            int count2 = fixture.SagaHarness.Consumed.Select<SimulatedAgvStateMachine.ScheduleSignalPosition>().Count();
            Assert.IsTrue(count == count2);

            // Last position message received should be the position of the last node
            var positionMessage = fixture.TestHarness.Published.Select<AgvPositionReceived>().Last().Context.Message;
            var destinationNode = graph.Nodes.First(n => n.Id == "D1");
            Assert.AreEqual(destinationNode.X, positionMessage.AgvPosition.X, 0.00001f);
            Assert.AreEqual(destinationNode.Y, positionMessage.AgvPosition.Y, 0.00001f);
            Assert.AreEqual(destinationNode.Z, positionMessage.AgvPosition.Z, 0.00001f);
        }
    }
}