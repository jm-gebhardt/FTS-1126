using System.Diagnostics;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// AgvStateMachine Test
    /// </summary>
    [TestClass]
    public class AgvStateMachineTest
    {
        /// <summary>
        /// When_AgvStatusReceived_NewAgvSagaCreated
        /// </summary>
        [TestMethod]
        public async Task When_AgvStatusReceived_NewAgvSagaCreated()
        {
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();

            const string agvId = "Order#1";
            var correlationId = agvId.ToGuid();
            var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);

            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.IsTrue(await fixture.TestHarness.Published.Any<ReleaseAgv>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<ReleaseAgv>());
            Assert.AreEqual(fixture.SagaHarness.StateMachine.Idle.Name, saga.CurrentState);
            Assert.AreEqual(agvId, saga.AgvId);
            Assert.IsNotNull(saga.AgvStatus);
            Assert.IsNull(saga.AgvGraph);
            Assert.IsNull(saga.AgvData);
        }

        /// <summary>
        /// When_AgvStatusReceived_EmptyCorrelationId_NoSagaCreated
        /// </summary>
        [TestMethod]
        public async Task When_AgvStatusReceived_EmptyCorrelationId_NoSagaCreated()
        {
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();

            const string agvId = "Order#1";
            var correlationId = Guid.Empty;
            var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);

            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsNull(await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10)));
        }

        /// <summary>
        /// When AgvReserve Requested Agv Is Idle AcceptAgvReservation
        /// </summary>
        [TestMethod]
        public async Task When_AgvReservationRequested_AgvIsIdle_AcceptAgvReservation()
        {
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(busRegistrationConfigurator: x => { x.AddRequestClient<ReserveAgv>(); });

            const string agvId = "Order#1";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            fixture.Add(agvStage);

            IRequestClient<ReserveAgv>? client = fixture.TestHarness.GetRequestClient<ReserveAgv>();
            Response response = await client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(
                new ReserveAgv(correlationId));
            Assert.IsInstanceOfType(response.Message, typeof(AgvReservationSucceeded));

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(fixture.Machine.Reserved.Name, saga.CurrentState);

        }

        /// <summary>
        /// When AgvReservation Requested Agv Is Idle AcceptAgvReservation
        /// </summary>
        [DataRow(nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Reserved))]
        [DataRow(nameof(AgvStateMachine.Busy))]
        [TestMethod]
        public async Task When_AgvReservationRequested_AgvIsBusyDiscoveredReserved_RejectAgvReservation(string acceptedStates)
        {

            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(busRegistrationConfigurator: x => { x.AddRequestClient<ReserveAgv>(); });

            const string agvId = "Order#1";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                acceptedStates,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            fixture.Add(agvStage);

            IRequestClient<ReserveAgv>? client = fixture.TestHarness.GetRequestClient<ReserveAgv>();
            Response response = await client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(
                new ReserveAgv(correlationId));

            Assert.IsInstanceOfType(response.Message, typeof(AgvReservationRejected));

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(acceptedStates, saga.CurrentState);
        }

        /// <summary>
        /// Checks if IAgvStatusReceived is consumed with LastNodeId
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("Node01")]
        public async Task ConsumeAgvStatusReceived_with_LastNodeId(string inputLastNode)
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false, inputLastNode)
            ));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.TestHarness.Consumed.Any<AgvStatusReceived>());
            var consumedMessage = fixture.TestHarness.Consumed.Select<AgvStatusReceived>().First().Context.Message;
            Assert.AreEqual(inputLastNode, consumedMessage.AgvStatus.LastNodeId);

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.IsNotNull(saga.AgvStatus);
            Assert.AreEqual(inputLastNode, saga.AgvStatus.LastNodeId);
        }

        /// <summary>
        /// Checks if RequestAgvGraph is sent because AGV has no graph
        /// </summary>
        [TestMethod]
        public async Task Emit_AgvRequestGraph_when_NoGraphIsGiven()
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvGraphReceivedLastTime: DateTime.Now - new TimeSpan(0, 0, 31)
            );

            fixture.Add(agvStage);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvGraph>());
            
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(DateTime.MinValue, saga.AgvDataReceivedLastTime);
        }

        /// <summary>
        /// Checks if RequestAgvGraph is not sent because AGV is already given a graph
        /// </summary>
        [TestMethod]
        public async Task DoNotEmit_AgvRequestGraph_when_GraphIsGiven()
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();

            var agvGraph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("node01"){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f},
                    new AgvGraph.Node("node02"){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 0f, Z = 0f}
                }, new[]
                {
                    new AgvGraph.Edge("node01", "node02"){ Metric = 4f}
                },
                Guid.Empty);

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvGraph: agvGraph,
                agvGraphReceivedLastTime: DateTime.Now - new TimeSpan(0, 0, 29)
            );

            fixture.Add(agvStage);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsFalse(await fixture.TestHarness.Published.Any<RequestAgvGraph>());
        }

        /// <summary>
        /// Checks if RequestAgvGraph is sent because there was no update in the past 24 hours
        /// </summary>
        [TestMethod]
        public async Task Emit_AgvRequestGraph_when_LastGraphUpdate_Is_Longer_Than_24Hours()
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();
            
            var agvGraph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("node01"){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f},
                    new AgvGraph.Node("node02"){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 0f, Z = 0f}
                }, new[]
                {
                    new AgvGraph.Edge("node01", "node02"){ Metric = 4f}
                },
                Guid.Empty);

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvGraph: agvGraph,
                agvGraphReceivedLastTime: DateTime.Now - new TimeSpan(24, 0, 1)
            );

            fixture.Add(agvStage);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvGraph>());
        }

        /// <summary>
        /// Checks if RequestAgvData sent because AGV has no data
        /// </summary>
        [TestMethod]
        public async Task Emit_AgvRequestData_when_NoDataIsGiven()
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvDataReceivedLastTime: DateTime.Now - new TimeSpan(0, 0, 31)
            );

            fixture.Add(agvStage);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvData>());

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(DateTime.MinValue, saga.AgvGraphReceivedLastTime);
        }

        /// <summary>
        /// Checks if RequestAgvData is not sent because AGV is already given its data
        /// </summary>
        [TestMethod]
        public async Task DoNotEmit_AgvRequestData_when_DataIsGiven()
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();
            
            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvData = new AgvData(correlationId, "Karis", "BF1", null, 1.8, 120.0, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvData: agvData,
                agvDataReceivedLastTime: DateTime.Now - new TimeSpan(0, 0, 29)
            );

            fixture.Add(agvStage);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            var token = new CancellationTokenSource();
            token.CancelAfter(10);
            Assert.IsFalse(await fixture.TestHarness.Published.Any<RequestAgvData>(token.Token));
        }

        /// <summary>
        /// Checks if RequestAgvData is sent because there was no update in the past 24 hours
        /// </summary>
        [TestMethod]
        public async Task Emit_AgvRequestData_when_LastDataUpdate_Is_Longer_Than_24Hours()
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();
            
            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvData = new AgvData(correlationId, "Karis", "BF1", null, 1.8, 120.0, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());
            
            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvData: agvData,
                agvDataReceivedLastTime: DateTime.Now - new TimeSpan(24, 0, 1)
            );

            fixture.Add(agvStage);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvData>());
        }

        /// <summary>
        /// Checks if RequestAgvData and RequestAgvGraph are sent in the initial state
        /// </summary>
        [TestMethod]
        public async Task Emit_AgvRequestData_and_AgvRequestGraph_in_Initial_State()
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();
            
            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Initial.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            fixture.Add(agvStage);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvData>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvGraph>());
        }

        /// <summary>
        /// Checks if RequestAgvData and RequestAgvGraph are sent in the initial state
        /// </summary>
        [TestMethod]
        public async Task Verify_DateTimes_On_Inbound_AgvRequestGraph_And_AgvRequestData()
        {
            // Arrange
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            fixture.Add(agvStage);

            var agvGraph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("node01"){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f},
                    new AgvGraph.Node("node02"){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 0f, Z = 0f}
                }, new[]
                {
                    new AgvGraph.Edge("node01", "node02"){ Metric = 4f}
                },
                Guid.Empty);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvGraphReceived(
                correlationId,
                agvId,
                agvGraph
            ));
            var graphReceived = DateTime.Now;


            var agvData = new AgvData(correlationId, "Karis", "BF1", null, 1.8, 120.0, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());

            await fixture.TestHarness.Bus.Publish(new AgvDataReceived(
                correlationId,
                agvId,
                agvData
            ));
            var dataReceived = DateTime.Now;
            
            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvGraphReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvGraphReceived>());

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvDataReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvDataReceived>());
            
            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            
            TimeSpan? graphTimeDifference = saga.AgvGraphReceivedLastTime - graphReceived;
            Assert.IsNotNull(graphTimeDifference);
            Trace.WriteLine(graphTimeDifference.Value);
            Assert.IsTrue(graphTimeDifference.Value.TotalSeconds < 1);

            TimeSpan? dataTimeDifference = saga.AgvDataReceivedLastTime - dataReceived;
            Assert.IsNotNull(dataTimeDifference);
            Trace.WriteLine(dataTimeDifference.Value);
            Assert.IsTrue(dataTimeDifference.Value.TotalSeconds < 1);
        }
    }
}