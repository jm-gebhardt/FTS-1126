using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services
{
    /// <summary>
    /// Tests for <see cref="AgvGraphBuilderStateMachine"/>.
    /// </summary>
    [TestClass]
    public class AgvGraphBuilderStateMachineTest
    {
        private const string AgvTestIdentifier01 = "#2410#";

        /// <summary>
        /// Test creation of saga instance.
        /// </summary>
        [TestMethod]
        public async Task When_RequestAgvGraphReceived_NewAgvGraphBuilderSagaCreated()
        {
            var correlationId = AgvTestIdentifier01.ToGuid();

            var fixture = new StateMachineTestFixture<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>();

            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvTestIdentifier01));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttRequestStationList>());
            Assert.AreEqual(fixture.SagaHarness.StateMachine.RequestAgvGraphReceived, saga.CurrentState);
            Assert.AreEqual(correlationId, saga.CorrelationId);
            Assert.IsNull(saga.MqttStationList);
            Assert.IsNull(saga.AgvGraph);
        }

        /// <summary>
        /// When_AgvStatusReceived_EmptyCorrelationId_NoSagaCreated
        /// </summary>
        [TestMethod]
        public async Task When_RequestAgvGraphReceived_EmptyCorrelationId_NoSagaCreated()
        {
            var correlationId = Guid.Empty;

            var fixture = new StateMachineTestFixture<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>();

            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvTestIdentifier01));
            Assert.IsFalse(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());

            Assert.IsNull(await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10)));
        }

        /// <summary>
        /// When AgvReserve Requested Agv Is Idle AcceptAgvReservation
        /// </summary>
        [TestMethod]
        public async Task When_AllStatesPassedByReceive_ValidGraphPublished()
        {
            var correlationId = AgvTestIdentifier01.ToGuid();

            var fixture = new StateMachineTestFixture<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>();

            // Publish RequestAgvGraph ...
            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvTestIdentifier01));
            // ... expect it's gonna be consumed by the statemachine.
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());

            // Get the instance which correlates to the AGV.
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            // Ensure that state machine did send MqttRequestStationList ...
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttRequestStationList>());
            // ... and the current state is RequestAgvGraphReceived.
            Assert.AreEqual(fixture.SagaHarness.StateMachine.RequestAgvGraphReceived, saga.CurrentState);
            Assert.IsNull(saga.MqttStationList);
            Assert.IsNull(saga.MqttGraph);
            Assert.IsNull(saga.AgvGraph);

            var requestIdList = saga.CurrentMqttRequestId; // Has been choosen by the state machine as request ID for MQTT.
            var stationList = new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow,
                new Station[] {
                    new(Function.Pick, "P01", new Position(1f, 2f, 3f), "42"),
                    new(Function.Drop, "D01", new Position(4f, 5f, 6f), "7")
                },
                requestIdList);
            // Publish StationListReceived ...
            await fixture.TestHarness.Bus.Publish(new InternalStationListReceived(correlationId, stationList));
            // ... expect it's gonna be consumed by the statemachine.
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<InternalStationListReceived>());

            // Ensure that state machine did send MqttRequestGraph ...
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttRequestGraph>());
            // ... and the current state is StationListReceived.
            Assert.AreEqual(fixture.SagaHarness.StateMachine.StationListReceived, saga.CurrentState);

            Assert.IsNotNull(saga.MqttStationList);
            Assert.IsNull(saga.MqttGraph);
            Assert.IsNull(saga.AgvGraph);
            Assert.AreEqual(correlationId, saga.MqttStationList.SerialNumber.ToGuid());
            Assert.AreEqual(2, saga.MqttStationList.Stations.Length);
            Assert.IsTrue(saga.MqttStationList.Stations.Any(x => x.Identifier == "P01"));
            var stationD02 = saga.MqttStationList.Stations.Single(x => x.Identifier == "D01");
            Assert.AreEqual("7", stationD02.TypeId);

            var requestIdGraph = saga.CurrentMqttRequestId; // Has been chosen by the state machine as request ID for MQTT.
            var graph = new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow,
                new Node[] {
                    new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                    new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
                },
                new Edge[] {
                    new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
                },
                string.Empty,
                requestIdGraph);
            // Publish GraphReceived ...
            await fixture.TestHarness.Bus.Publish(new InternalGraphReceived(correlationId, graph));
            // ... expect it's gonna be consumed by the state machine.
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<InternalGraphReceived>());

            Assert.AreNotEqual(requestIdList, requestIdGraph);

            // Check if the state machine published AgvGraphReceived finally.
            var publishedData = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;

            Assert.AreEqual(correlationId, publishedData.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier01.ToGuid(), publishedData.Graph.CorrelationId);
            // 2 stations + 2 nodes
            Assert.AreEqual(2 + 2, publishedData.Graph.Nodes.Length);
            // 4 edges (2 (bidirectional) per station + 2 edges (bidirectional) from graph
            Assert.AreEqual(2 + 4, publishedData.Graph.Edges.Length);
        }

        /// <summary>
        /// When graph is invald (e.g. duplicate node names) no graph will be stored
        /// </summary>
        [TestMethod]
        public async Task When_InvalidStationListReceive_InvalidGraphPublished()
        {
            var correlationId = AgvTestIdentifier01.ToGuid();

            var fixture = new StateMachineTestFixture<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>();

            // Publish RequestAgvGraph ...
            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvTestIdentifier01));
            // ... expect it's gonna be consumed by the statemachine.
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());

            // Get the instance which correlates to the AGV.
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            // Ensure that state machine did send MqttRequestStationList ...
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttRequestStationList>());
            // ... and the current state is RequestAgvGraphReceived.
            Assert.AreEqual(fixture.SagaHarness.StateMachine.RequestAgvGraphReceived, saga.CurrentState);

            var requestIdList = saga.CurrentMqttRequestId; // Has been choosen by the state machine as request ID for MQTT.
            var stationList = new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow,
                new Station[] {
                    new(Function.Pick, "P01", new Position(1f, 2f, 3f), "42"),
                    new(Function.Drop, "D01", new Position(4f, 5f, 6f), "7"),
                    new(Function.Drop, "D01", new Position(4f, 5f, 6f), "7")    // Duplicate
                },
                requestIdList);
            // Publish StationListReceived ...
            await fixture.TestHarness.Bus.Publish(new InternalStationListReceived(correlationId, stationList));
            // ... expect it's gonna be consumed by the statemachine.
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<InternalStationListReceived>());

            // Ensure that state machine did send MqttRequestGraph ...
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttRequestGraph>());
            // ... and the current state is StationListReceived.
            Assert.AreEqual(fixture.SagaHarness.StateMachine.StationListReceived, saga.CurrentState);

            var requestIdGraph = saga.CurrentMqttRequestId; // Has been chosen by the state machine as request ID for MQTT.
            var graph = new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow,
                new Node[] {
                    new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                    new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
                },
                new Edge[] {
                    new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
                },
                string.Empty,
                requestIdGraph);
            // Publish GraphReceived ...
            await fixture.TestHarness.Bus.Publish(new InternalGraphReceived(correlationId, graph));
            // ... expect it's gonna be consumed by the state machine.
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<InternalGraphReceived>());

            // Check if the state machine published AgvGraphReceived finally.
            var publishedData = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;

            // Check for invalid usability due to duplicate nodes.
            Assert.AreEqual(Common.Messages.AgvManagement.AgvGraph.GraphUsability.InvalidStations, publishedData.Graph.Usability);
        }
    }
}
