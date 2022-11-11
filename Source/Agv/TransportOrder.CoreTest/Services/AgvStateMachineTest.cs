using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    [TestClass]
    public class AgvStateMachineTest
    {
        /// <summary>
        /// Modified settings of state machine for testing
        /// </summary>
        /// <param name="AgvOfflineTimeoutMsWhileIdle"></param>
        /// <param name="AgvOfflineTimeoutMsWhileBusy"></param>
        private record Settings(int AgvOfflineTimeoutMsWhileIdle, int AgvOfflineTimeoutMsWhileBusy) : AgvStateMachine.ISettings;

        #region Setup

        /// <summary>
        /// Create AgvStateMachine test fixture with mocked services
        /// </summary>
        private StateMachineTestFixture<AgvStateMachine, AgvSaga> GetFixture(
            Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
            Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
            AgvStateMachine.ISettings? settings = null,
            TimeSpan? inactivityTimeout = null)
        {
            var mockRegistry = new Mock<IMapRegistry>();
            mockRegistry.Setup(s => s.GetAllMaps()).Returns(new EnumerableQuery<Map>(new List<Map>()));

            var mockObserver = new Mock<IStateObserver<AgvSaga>>();

            return new StateMachineTestFixture<AgvStateMachine, AgvSaga>(
                busRegistrationConfigurator: busRegistrationConfigurator,
                services: s =>
                {
                    s.AddSingleton(_ => mockObserver.Object);
                    s.AddTransient(_ => mockRegistry.Object);
                    s.AddSingleton(_ => new ProductionOptions()
                    {
                        ApplicationName = null,
                        AutoAccept = true
                    });
                    if (settings != null)
                    {
                        s.AddSingleton(_ => settings);
                    }
                },
                inMemoryConfiguration: inMemoryConfiguration,
                inactivityTimeout: inactivityTimeout);
        }

        /// <summary>
        /// Create a standardized AgvSaga for testing purposes
        /// </summary>
        /// <param name="previousState"></param>
        private AgvSaga CreateTestAgvSaga(string previousState)
        {
            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                currentState: previousState,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            return agvSaga;
        }

        /// <summary>
        /// Create a standardized AgvGraph for testing purposes
        /// </summary>
        private AgvGraph CreateTestAgvGraph()
        {
            const string mapName = "Map#1";
            var agvGraph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node("node01"){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f},
                    new AgvGraph.Node("node02"){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f}
                }, new[]
                {
                    new AgvGraph.Edge("node01", "node02"){ Metric = 4f}
                },
                mapName.ToGuid(),
                mapName);

            return agvGraph;
        }

        /// <summary>
        /// Create a standardized AgvData for testing purposes
        /// </summary>
        private AgvData CreateTestAgvData()
        {
            return new AgvData(Guid.Empty, "Karis", "BF 2", null, 80, 1.2, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());
        }

        #endregion

        #region State Transitions

        private const string TestAgvId = "AGV#1";

        private async Task TransitionStateWhenMessageReceived<TRequest>(TRequest message, string initialState, string resultingState) where TRequest : class, CorrelatedBy<Guid>
        {
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture();

            var correlationId = message.CorrelationId;

            var agvStage = new AgvSaga(
                correlationId,
                TestAgvId,
                initialState,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            fixture.Add(agvStage);

            await fixture.TestHarness.Bus.Publish(message);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<TRequest>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<TRequest>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(resultingState, saga.CurrentState);
        }

        [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Discovered), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Reserved), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.PendingLock), nameof(AgvStateMachine.Locked))]
        [DataRow(nameof(AgvStateMachine.Locked), nameof(AgvStateMachine.Locked))]
        [TestMethod]
        public async Task Transition_State_When_Message_Received_ReleaseAgv(string initialState, string resultingState)
        {
            var message = new ReleaseAgv(TestAgvId.ToGuid());
            await TransitionStateWhenMessageReceived(message, initialState, resultingState);
        }

        [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Discovered), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Reserved), nameof(AgvStateMachine.Reserved))]
        [DataRow(nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Busy))]
        [DataRow(nameof(AgvStateMachine.PendingLock), nameof(AgvStateMachine.PendingLock))]
        [DataRow(nameof(AgvStateMachine.Locked), nameof(AgvStateMachine.Idle))]
        [TestMethod]
        public async Task Transition_State_When_Message_Received_ForceReleaseAgv(string initialState, string resultingState)
        {
            var message = new ForceReleaseAgv(TestAgvId.ToGuid());
            await TransitionStateWhenMessageReceived(message, initialState, resultingState);
        }

        [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Reserved))]
        [DataRow(nameof(AgvStateMachine.Discovered), nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Reserved), nameof(AgvStateMachine.Reserved))]
        [DataRow(nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Busy))]
        [DataRow(nameof(AgvStateMachine.PendingLock), nameof(AgvStateMachine.PendingLock))]
        [DataRow(nameof(AgvStateMachine.Locked), nameof(AgvStateMachine.Locked))]
        [TestMethod]
        public async Task Transition_State_When_Message_Received_ReserveAgv(string initialState, string resultingState)
        {
            var message = new ReserveAgv(TestAgvId.ToGuid());
            await TransitionStateWhenMessageReceived(message, initialState, resultingState);
        }

        [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Locked))]
        [DataRow(nameof(AgvStateMachine.Discovered), nameof(AgvStateMachine.Locked))]
        [DataRow(nameof(AgvStateMachine.Reserved), nameof(AgvStateMachine.PendingLock))]
        [DataRow(nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.PendingLock))]
        [DataRow(nameof(AgvStateMachine.PendingLock), nameof(AgvStateMachine.PendingLock))]
        [DataRow(nameof(AgvStateMachine.Locked), nameof(AgvStateMachine.Locked))]
        [TestMethod]
        public async Task Transition_State_When_Message_Received_LockAgv(string initialState, string resultingState)
        {
            var message = new LockAgv(TestAgvId.ToGuid());
            await TransitionStateWhenMessageReceived(message, initialState, resultingState);
        }

        #endregion

        [TestMethod]
        public async Task When_AgvStatusReceived_NewAgvSagaCreated()
        {
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture();

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

        [TestMethod]
        public async Task When_AgvStatusReceived_EmptyCorrelationId_NoSagaCreated()
        {
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture();

            const string agvId = "AGV#1";
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
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture(x => { x.AddRequestClient<ReserveAgv>(); });

            const string agvId = "AGV#1";
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

        [DataRow(nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Reserved))]
        [DataRow(nameof(AgvStateMachine.Busy))]
        [TestMethod]
        public async Task When_AgvReservationRequested_AgvIsBusyDiscoveredReserved_RejectAgvReservation(string acceptedStates)
        {
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture(x => { x.AddRequestClient<ReserveAgv>(); });

            const string agvId = "AGV#1";
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
        /// Checks if AgvStatusReceived is consumed with LastNodeId
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        [DataRow("Node01")]
        public async Task ConsumeAgvStatusReceived_with_LastNodeId(string inputLastNode)
        {
            // Arrange
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture(
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler());

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
        /// Checks if requests are initially sent because AGV has no graph/data
        /// </summary>
        [TestMethod]
        public async Task When_GraphAndDataMissingInitially_BothAreRequested()
        {
            // Arrange
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            );
            // Request timeout already passed
            var utcStart = DateTime.UtcNow;
            // agvStage.AgvGraphRequestedLastTime and agvStage.AgvDataRequestedLastTime initially will result in max timespan

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
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvGraph>());

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            // Publishing requests should update corresponding last requested timestamp
            ((DateTime)saga.AgvDataRequestedLastTime!).ShouldBeGreaterThan(utcStart);
            ((DateTime)saga.AgvDataRequestedLastTime!).ShouldBeLessThan(DateTime.UtcNow);
            ((DateTime)saga.AgvGraphRequestedLastTime!).ShouldBeGreaterThan(utcStart);
            ((DateTime)saga.AgvGraphRequestedLastTime!).ShouldBeLessThan(DateTime.UtcNow);
        }

        /// <summary>
        /// Checks if requests are sent because AGV has no graph/data and timeout exceeded
        /// </summary>
        [TestMethod]
        public async Task When_GraphAndDataMissingAndTimeoutExceeded_BothAreRequested()
        {
            // Arrange
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            );
            // Request timeout already exceeded
            var utcStart = DateTime.UtcNow;
            // agvStage.AgvDataRequestedLastTime is max
            agvStage.AgvGraphRequestedLastTime = utcStart - new TimeSpan(0, 0, fixture.Machine.AgvRequestTimeoutS + 1);

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
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvGraph>());

            Assert.AreEqual(correlationId, await fixture.SagaHarness.Exists(correlationId));
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            // Publishing requests should update corresponding last requested timestamp
            Assert.IsTrue(saga.AgvDataRequestedLastTime > utcStart && saga.AgvDataRequestedLastTime < DateTime.UtcNow);
            Assert.IsTrue(saga.AgvGraphRequestedLastTime > utcStart && saga.AgvGraphRequestedLastTime < DateTime.UtcNow);
        }

        // ReSharper disable MethodSupportsCancellation
        /// <summary>
        /// Checks if only RequestAgvData is sent because AGV already has graph
        /// </summary>
        [TestMethod]
        public async Task When_GraphIsGivenAndDataMissing_OnlyAgvDataRequested()
        {
            // Arrange
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture(null, null, null, TimeSpan.FromSeconds(5));
            var token = new CancellationTokenSource();

            var agvGraph = CreateTestAgvGraph();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvGraph: agvGraph
            )
            {
                // Repeat request exceeded but no daily request
                AgvGraphRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, fixture.Machine.AgvRequestTimeoutS + 1),
                AgvDataRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, fixture.Machine.AgvRequestTimeoutS + 1)
            };

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
            token.CancelAfter(10);
            Assert.IsFalse(await fixture.TestHarness.Published.Any<RequestAgvGraph>(token.Token));
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvData>());
        }

        // ReSharper disable MethodSupportsCancellation
        /// <summary>
        /// Checks if only RequestAgvGraph is sent because AGV already has data
        /// </summary>
        [TestMethod]
        public async Task When_DataIsGivenAndGraphMissing_OnlyAgvGraphRequested()
        {
            // Arrange
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture(null, null, null, TimeSpan.FromSeconds(5));
            var token = new CancellationTokenSource();

            var agvData = CreateTestAgvData();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvData: agvData
            )
            {
                // Repeat request exceeded but no daily request
                AgvGraphRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, fixture.Machine.AgvRequestTimeoutS + 1),
                AgvDataRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, fixture.Machine.AgvRequestTimeoutS + 1)
            };

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
            token.CancelAfter(10);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvGraph>());
            Assert.IsFalse(await fixture.TestHarness.Published.Any<RequestAgvData>(token.Token));
        }

        /// <summary>
        /// Checks if requests are repeated once a day
        /// </summary>
        [TestMethod]
        public async Task When_GraphAndDataReceivedOneDayAgo_RequestBothAgain()
        {
            // Arrange
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture();

            var agvGraph = CreateTestAgvGraph();
            var agvData = CreateTestAgvData();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvGraph: agvGraph,
                agvData: agvData
            )
            {
                AgvGraphRequestedLastTime = DateTime.UtcNow - new TimeSpan(24, 0, 1),
                AgvDataRequestedLastTime = DateTime.UtcNow - new TimeSpan(24, 0, 1)
            };

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
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestAgvData>());
        }

        /// <summary>
        /// Checks if requests are sent in the initial state
        /// </summary>
        [TestMethod]
        public async Task When_AgvInInitialState_RequestIsDoneUnconditionally()
        {
            // Arrange
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Initial.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            {
                AgvGraphRequestedLastTime = DateTime.UtcNow,
                AgvDataRequestedLastTime = DateTime.UtcNow
            };

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
        /// Checks if RequestAgvMap is not sent as soon as the corresponding RelatedMapId is found in the registry
        /// </summary>
        [DataRow("Discovered")]
        [DataRow("Idle")]
        [DataRow("Reserved")]
        [DataRow("Busy")]
        [TestMethod]
        public async Task RequestAgvMap_NotSent_when_RelatedMapName_found_in_Registry(string previousState)
        {
            // Arrange
            var mockRegistry = new Mock<IMapRegistry>();
            mockRegistry.Setup(s => s.GetAllMaps()).Returns(new EnumerableQuery<Map>(new List<Map>()));

            var mockObserver = new Mock<IStateObserver<AgvSaga>>();

            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(services: s =>
            {
                s.AddTransient(_ => mockRegistry.Object);
                s.AddSingleton(mockObserver.Object);
                s.AddSingleton(_ => new ProductionOptions()
                {
                    ApplicationName = null,
                    AutoAccept = true
                });
            });

            var agvSaga = CreateTestAgvSaga(previousState);    // The previous state is variable due to DuringAny()
            fixture.Add(agvSaga);

            var agvGraph = CreateTestAgvGraph();

            mockRegistry.Setup(mo => mo.Exists(agvGraph.RelatedMapId)).Returns(true);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvGraphReceived(agvSaga.CorrelationId, agvSaga.AgvId, agvGraph));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvGraphReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvGraphReceived>());
            Assert.IsFalse(await fixture.TestHarness.Published.Any<RequestAgvMap>());
        }

        /// <summary>
        /// Checks if RequestAgvMap is received assuming that the registry is empty
        /// </summary>
        /// <remarks>When there is no match, a registry insertion must be invoked.</remarks>
        [DataRow("Discovered")]
        [DataRow("Idle")]
        [DataRow("Reserved")]
        [DataRow("Busy")]
        [TestMethod]
        public async Task AgvMapReceived_is_Consumed_and_Processed_with_EmptyRegistry(string previousState)
        {
            // Arrange
            var mockRegistry = new Mock<IMapRegistry>();
            mockRegistry.Setup(s => s.GetAllMaps()).Returns(new EnumerableQuery<Map>(new List<Map>()));

            var mockObserver = new Mock<IStateObserver<AgvSaga>>();

            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(services: s =>
            {
                s.AddTransient(_ => mockRegistry.Object);
                s.AddSingleton(mockObserver.Object);
                s.AddSingleton(_ => new ProductionOptions()
                {
                    ApplicationName = null,
                    AutoAccept = true
                });
            });

            var agvSaga = CreateTestAgvSaga(previousState);    // The previous state is variable due to DuringAny()
            fixture.Add(agvSaga);

            const string mapId = "basicMap";
            const string imageData = "bGlnaHQgd29yay4=";
            const float resolution = 1;
            const float xOffset = 1;
            const float yOffset = 1;
            var modificationDate = DateTime.Now;

            mockRegistry.Setup(mo => mo.AddOrUpdate(mapId.ToGuid(), mapId, imageData, resolution, xOffset, yOffset, modificationDate)).Returns(true);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvMapReceived(
                agvSaga.CorrelationId,
                agvSaga.AgvId,
                new AgvMap(mapId.ToGuid(), mapId, modificationDate, imageData, resolution, xOffset, yOffset)));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvMapReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvMapReceived>());
            Assert.AreEqual(agvSaga.CorrelationId, await fixture.SagaHarness.Exists(agvSaga.CorrelationId));

            mockRegistry.Verify(mo => mo.AddOrUpdate(mapId.ToGuid(), mapId, imageData, resolution, xOffset, yOffset, modificationDate), Times.Once);
        }

        /// <summary>
        /// Checks if RequestAgvMap is received assuming that the registry already contains a matching entry
        /// </summary>
        /// <remarks>When there is a match, any registry insertion must not happen.</remarks>
        [TestMethod]
        [DataRow("Discovered")]
        [DataRow("Idle")]
        [DataRow("Reserved")]
        [DataRow("Busy")]
        public async Task AgvMapReceived_is_Consumed_and_Processed_with_Registry_containing_entries(string previousState)
        {
            // Arrange
            var mockRegistry = new Mock<IMapRegistry>();
            mockRegistry.Setup(s => s.GetAllMaps()).Returns(new EnumerableQuery<Map>(new List<Map>()));

            var mockObserver = new Mock<IStateObserver<AgvSaga>>();

            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(services: s =>
            {
                s.AddTransient(_ => mockRegistry.Object);
                s.AddSingleton(mockObserver.Object);
                s.AddSingleton(_ => new ProductionOptions()
                {
                    ApplicationName = null,
                    AutoAccept = true
                });
            });

            var agvSaga = CreateTestAgvSaga(previousState);    // The previous state is variable due to DuringAny()
            fixture.Add(agvSaga);

            const string mapId = "basicMap";
            const string imageData = "bGlnaHQgd29yay4=";
            const float resolution = 1;
            const float xOffset = 1;
            const float yOffset = 1;
            var modificationDate = DateTime.Now;

            mockRegistry.Setup(mo => mo.Exists(mapId.ToGuid())).Returns(true);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvMapReceived(
                agvSaga.CorrelationId,
                agvSaga.AgvId,
                new AgvMap(mapId.ToGuid(), mapId, modificationDate, imageData, resolution, xOffset, yOffset)));

            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvMapReceived>());
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvMapReceived>());
            Assert.AreEqual(agvSaga.CorrelationId, await fixture.SagaHarness.Exists(agvSaga.CorrelationId));

            mockRegistry.Verify(mo => mo.AddOrUpdate(mapId.ToGuid(), mapId, imageData, resolution, xOffset, yOffset, modificationDate), Times.Once);
        }

        /// <summary>
        /// When AgvSaga state has changed check if observer was triggered
        /// </summary>
        [TestMethod]
        public async Task When_AgvSaga_State_Changed_Trigger_Observer()
        {
            var mockRegistry = new Mock<IMapRegistry>();
            mockRegistry.Setup(s => s.GetAllMaps()).Returns(new EnumerableQuery<Map>(new List<Map>()));

            var mockObserver = new Mock<IStateObserver<AgvSaga>>();

            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(
                services: s =>
                {
                    s.AddSingleton(_ => mockObserver.Object);
                    s.AddTransient(_ => mockRegistry.Object);
                    s.AddSingleton(_ => new ProductionOptions()
                    {
                        ApplicationName = null,
                        AutoAccept = true
                    });
                });

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var stateBefore = fixture.Machine.Idle;
            var stateAfter = fixture.Machine.Reserved;

            var agvStage = new AgvSaga(
                correlationId,
                agvId,
                stateBefore.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            fixture.Add(agvStage);

            IRequestClient<ReserveAgv>? client = fixture.TestHarness.GetRequestClient<ReserveAgv>();
            Response response = await client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(
                new ReserveAgv(correlationId));
            Assert.IsInstanceOfType(response.Message, typeof(AgvReservationSucceeded));

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(stateAfter.Name, saga.CurrentState);

            mockObserver.Verify(s => s.StateChanged(It.IsAny<BehaviorContext<AgvSaga>>(), stateAfter, stateBefore), Times.Once);
        }

        /// <summary>
        /// When AgvInitializedMessage is received AgvfflineWatchdog is started
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task When_AgvInitializedMessage_AgvOfflineWatchdogIsScheduled(bool agvInitialAlive)
        {
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture(
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler());

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var agv = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            { Alive = agvInitialAlive };
            fixture.Add(agv);

            await fixture.TestHarness.Bus.Publish(new AgvStateMachine.AgvInitializeMessage(correlationId, agvId));
            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            // Double check for initial alive (event when saga taken from the state machine)
            Assert.AreEqual(agvInitialAlive, agv.Alive);

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStateMachine.AgvInitializeMessage>());
            Assert.IsTrue(fixture.TestHarness.Sent.Select<AgvStateMachine.AgvOfflineMessage>().Any());
        }

        /// <summary>
        /// When watchdog expires AGV goes offline
        /// </summary>
        [TestMethod]
        public async Task When_AgvWatchdogExpires_AgvWentOffline()
        {
            const int agvOfflineTimeoutMsWhileIdle = 100;
            const int agvOfflineTimeoutMsWhileBusy = 500;

            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture(
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler(),
                new Settings(agvOfflineTimeoutMsWhileIdle, agvOfflineTimeoutMsWhileBusy),
                TimeSpan.FromMilliseconds(2 * Math.Max(agvOfflineTimeoutMsWhileIdle, agvOfflineTimeoutMsWhileBusy)));

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var agv = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            { Alive = true };
            fixture.Add(agv);

            await fixture.TestHarness.Bus.Publish(new AgvStateMachine.AgvInitializeMessage(correlationId, agvId));

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStateMachine.AgvInitializeMessage>());
            Assert.IsTrue(fixture.TestHarness.Sent.Select<AgvStateMachine.AgvOfflineMessage>().Any());
            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.IsTrue(agv.Alive);

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStateMachine.AgvOfflineMessage>());
            var now = DateTime.UtcNow;
            var msg = fixture.SagaHarness.Consumed.Select<AgvStateMachine.AgvOfflineMessage>().First().Context.Message;
            // When triggerd initially by AgvInitializeMessage the timeout for busy state is taken into account
            Assert.IsTrue((now - msg.Created).TotalMilliseconds > 0.9 * agvOfflineTimeoutMsWhileBusy);
            Assert.IsTrue((now - msg.Created).TotalMilliseconds < 1.5 * agvOfflineTimeoutMsWhileBusy);

            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.IsFalse(agv.Alive);
        }

        [TestMethod]
        public async Task When_AlternateGameBetweenWatchdogAndStatus_AgvTogglesOfflineAndOnline()
        {
            const int agvOfflineTimeoutMsWhileIdle = 500;
            const int agvOfflineTimeoutMsWhileBusy = 1000;

            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = GetFixture(
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler(),
                new Settings(agvOfflineTimeoutMsWhileIdle, agvOfflineTimeoutMsWhileBusy),
                TimeSpan.FromMilliseconds(2 * Math.Max(agvOfflineTimeoutMsWhileIdle, agvOfflineTimeoutMsWhileBusy)));

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var agv = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            { Alive = true };
            fixture.Add(agv);

            var agvStatus1 = new AgvStatus(correlationId, "first", false, true, 60, false, false);
            var agvStatus2 = new AgvStatus(correlationId, "second", false, true, 60, false, false);

            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus1));

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>());
            Assert.IsTrue(fixture.TestHarness.Sent.Select<AgvStateMachine.AgvOfflineMessage>().Any());

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStateMachine.AgvOfflineMessage>());
            var now = DateTime.UtcNow;
            var msg = fixture.SagaHarness.Consumed.Select<AgvStateMachine.AgvOfflineMessage>().First().Context.Message;
            // When triggerd by AgvStatusReceived the timeout depends on the state (and this AGV is idle)
            Assert.IsTrue((now - msg.Created).TotalMilliseconds > 0.9 * agvOfflineTimeoutMsWhileIdle);
            Assert.IsTrue((now - msg.Created).TotalMilliseconds < 1.5 * agvOfflineTimeoutMsWhileIdle);

            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.IsFalse(agv.Alive);

            // Make sure nobody did spoil the test
            Assert.IsTrue(agvStatus1.OrderId != agvStatus2.OrderId);

            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus2));
            // We need to wait for the "second" message (don't check just for any because a first message is already there)
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>(x => x.Context.Message.AgvStatus.OrderId == "second"));

            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.IsTrue(agv.Alive);
        }
    }
}