using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Extensions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Agvs
{
    [TestClass]
    public class AgvStateMachineTest
    {
        #region Setup

        private class AgvStateMachineTestFixture
        {
            public Mock<IAgvDataRegistry> AgvDataRegistry { get; }
            public Mock<IMapRegistry> MockMapRegistry { get; }
            private Mock<IGraphRegistry> MockGraphRegistry { get; }
            public Mock<IStateObserver<AgvSaga>> MockObserver { get; }
            public StateMachineTestFixture<AgvStateMachine, AgvSaga> Fixture { get; }

            private Dictionary<Guid, AgvData> Agvdata { get; } = new();

            /// <summary>
            /// Create AgvStateMachine test fixture with mocked services
            /// </summary>
            public AgvStateMachineTestFixture(
                Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null,
                Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? inMemoryConfiguration = null,
                AgvSettings? settings = null,
                TimeSpan? inactivityTimeout = null)
            {
                AgvDataRegistry = new Mock<IAgvDataRegistry>();
                MockMapRegistry = new Mock<IMapRegistry>();
                MockGraphRegistry = new Mock<IGraphRegistry>();
                MockObserver = new Mock<IStateObserver<AgvSaga>>();

                MockMapRegistry.Setup(s => s.GetAllMaps()).Returns(new EnumerableQuery<Map>(new List<Map>()));

                AgvDataRegistry.Setup(x => x.Exists(It.IsAny<Guid>())).Returns<Guid>(correlationId => Agvdata.ContainsKey(correlationId));
                AgvDataRegistry.Setup(x => x.GetAgvData(It.IsAny<Guid>()))
                    .Returns<Guid>(correlationId =>
                    {
                        Agvdata.TryGetValue(correlationId, out var agvData);
                        return agvData;
                    });
                AgvDataRegistry.Setup(x => x.AddOrUpdate(It.IsAny<AgvData>()))
                    .Returns<AgvData>(agvData =>
                    {
                        Agvdata[agvData.CorrelationId] = agvData;
                        return true;
                    });

                Fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(
                    busRegistrationConfigurator: busRegistrationConfigurator,
                    services: s =>
                    {
                        s.AddSingleton(_ => AgvDataRegistry.Object);
                        s.AddSingleton(_ => MockMapRegistry.Object);
                        s.AddSingleton(_ => MockGraphRegistry.Object);
                        s.AddSingleton(_ => MockObserver.Object);
                        s.AddSingleton(_ => new ProductionOptions()
                        {
                            ApplicationName = null,
                            AutoAccept = true
                        });
                        s.AddAgvSettings(settings);
                    },
                    inMemoryConfiguration: inMemoryConfiguration,
                    inactivityTimeout: inactivityTimeout);
            }
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
        /// Create a standardized AgvMap for testing purposes
        /// </summary>
        private AgvMap CreateTestAgvMap()
        {
            const string mapId = "basicMap";
            const string imageData = "bGlnaHQgd29yay4=";
            const float resolution = 1;
            const float xOffset = 1;
            const float yOffset = 1;
            var modificationDate = DateTime.UtcNow;

            return new AgvMap(mapId.ToGuid(), mapId, modificationDate, imageData, resolution, xOffset, yOffset);
        }

        #endregion

        #region State Transitions

        private const string TestAgvId = "AGV#1";

        private async Task TransitionStateWhenMessageReceived<TRequest>(
            TRequest message,
            string initialState,
            string resultingState,
            bool assertAreEqual = true)
            where TRequest : class, CorrelatedBy<Guid>
        {
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            var correlationId = message.CorrelationId;

            var agvSaga = new AgvSaga(
                correlationId,
                TestAgvId,
                initialState,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            );

            fixture.Add(agvSaga);

            await fixture.TestHarness.Bus.Publish(message);

            (await fixture.TestHarness.Published.Any<TRequest>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<TRequest>()).ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            if (assertAreEqual)
            {
                saga.CurrentState.ShouldBe(resultingState);
            }
            else
            {
                saga.CurrentState.ShouldNotBe(resultingState);
            }
        }

        private async Task TransitionStateOnRequest<TRequest, TExpectedResponse, TUnexpectedResponse>(TRequest request, string initialSate, string resultingState) 
            where TRequest : class, CorrelatedBy<Guid>
            where TExpectedResponse : class
            where TUnexpectedResponse : class
        {
            var agvStateMachine = new AgvStateMachineTestFixture(x => { x.AddRequestClient<TRequest>(); });
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            var correlationId = request.CorrelationId;
            string agvId = correlationId.ToString();

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                initialSate,
                new AgvStatus(correlationId, null, true, true, 10, true, true, "R1"),
                new AgvGraph(correlationId, Array.Empty<AgvGraph.Node>(), Array.Empty<AgvGraph.Edge>(), Guid.Empty, "EmptyMap")
            );
 
            fixture.Add(agvSaga);

            IRequestClient<TRequest>? client = fixture.TestHarness.GetRequestClient<TRequest>();
            Response response = await client.GetResponse<TExpectedResponse, TUnexpectedResponse>(request);
            response.Message.ShouldBeAssignableTo(typeof(TExpectedResponse));

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.CurrentState.ShouldBe(resultingState);
        }  
        

        [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Charging), nameof(AgvStateMachine.Charging))]
        [TestMethod]
        public async Task Transition_State_Should_Fail_When_Message_Received_SetAgvIdle(string initialState, string resultingState)
        {
            var message = new SetAgvIdle(TestAgvId.ToGuid());
            await TransitionStateWhenMessageReceived(message, initialState, resultingState);
        }

        [DataRow(nameof(AgvStateMachine.Discovered), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Idle), nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Busy), nameof(AgvStateMachine.Idle))]
        [TestMethod]
        public async Task Transition_State_When_Message_Received_ForceReleaseAgv(string initialState, string resultingState)
        {
            var message = new ForceReleaseAgv(TestAgvId.ToGuid());
            await TransitionStateWhenMessageReceived(message, initialState, resultingState);
        }


        #endregion

        [TestMethod]
        public async Task When_AgvStatusReceived_NewAgvSagaCreated()
        {
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);

            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.CurrentState.ShouldBe(fixture.SagaHarness.StateMachine.Discovered.Name);
            saga.AgvId.ShouldBe(agvId);
            saga.AgvStatus.ShouldNotBeNull();
            saga.AgvGraph.ShouldBeNull();
        }

        [TestMethod]
        public async Task When_AgvStatusReceived_EmptyCorrelationId_NoSagaCreated()
        {
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV#1";
            var correlationId = Guid.Empty;
            var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);

            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus));

            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.NotExists(Guid.Empty, TimeSpan.FromMilliseconds(10))).ShouldBeNull();
        }
        
                /// <summary>
        /// When AgvReserve Requested Agv Is Idle AcceptAgvReservation
        /// </summary>
        [DataRow(nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Busy))]
        [TestMethod]
        public async Task When_SetAgvIdleRequest_AcceptAgvSetIdle(string initialState)
        {
            await TransitionStateOnRequest<SetAgvIdle, SetAgvIdleSucceeded ,SetAgvIdleRejected>(new SetAgvIdle("AGV#1".ToGuid()),initialState, nameof(AgvStateMachine.Idle));
        }

        [DataRow(nameof(AgvStateMachine.Charging))]
        [DataRow(nameof(AgvStateMachine.Idle))]
        [TestMethod]
        public async Task When_SetAgvIdleRequest_RejectAgvSetIdle(string initialState)
        {
            await TransitionStateOnRequest<SetAgvIdle, SetAgvIdleRejected, SetAgvIdleSucceeded>(new SetAgvIdle("AGV#1".ToGuid()),initialState, initialState);
        }
        
        [DataRow(nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Charging))]
        [TestMethod]
        public async Task When_SetAgvBusyRequest_AcceptAgvSetBusy(string initialState)
        {
              await TransitionStateOnRequest<SetAgvBusy, SetAgvBusySucceeded, SetAgvBusyRejected>(new SetAgvBusy("AGV#1".ToGuid()),initialState, nameof(AgvStateMachine.Busy));
        }

        [DataRow(nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Busy))]
        [TestMethod]
        public async Task When_SetAgvBusyRequest_RejectAgvSetBusy(string initialState)
        {
            await TransitionStateOnRequest<SetAgvBusy,SetAgvBusyRejected, SetAgvBusySucceeded>(new SetAgvBusy("AGV#1".ToGuid()),initialState, initialState);
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
            var agvStateMachine = new AgvStateMachineTestFixture(
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler());
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false, inputLastNode)
            ));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.TestHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();
            var consumedMessage = fixture.TestHarness.Consumed.Select<AgvStatusReceived>().First().Context.Message;
            consumedMessage.AgvStatus.LastNodeId.ShouldBe(inputLastNode);

            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.AgvStatus.ShouldNotBeNull();
            saga.AgvStatus.LastNodeId.ShouldBe(inputLastNode);
        }

        /// <summary>
        /// Checks if requests are initially sent because AGV has no graph/data
        /// </summary>
        [TestMethod]
        public async Task When_GraphAndDataMissingInitially_BothAreRequested()
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            );
            // Request timeout already passed
            var utcStart = DateTime.UtcNow;
            // agvSaga.AgvGraphRequestedLastTime and agvSaga.AgvDataRequestedLastTime initially will result in max timespan

            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

            (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();

            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
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
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            );
            // Request timeout already exceeded
            var utcStart = DateTime.UtcNow;
            // agvSaga.AgvDataRequestedLastTime is max
            agvSaga.AgvGraphRequestedLastTime = utcStart - new TimeSpan(0, 0, fixture.Machine.AgvRequestTimeoutS + 1);

            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();

            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            // Publishing requests should update corresponding last requested timestamp
            (saga.AgvDataRequestedLastTime > utcStart && saga.AgvDataRequestedLastTime < DateTime.UtcNow).ShouldBeTrue();
            (saga.AgvGraphRequestedLastTime > utcStart && saga.AgvGraphRequestedLastTime < DateTime.UtcNow).ShouldBeTrue();
        }

        // ReSharper disable MethodSupportsCancellation
        /// <summary>
        /// Checks if only RequestAgvData is sent because AGV already has graph
        /// </summary>
        [TestMethod]
        public async Task When_GraphIsGivenAndDataMissing_OnlyAgvDataRequested()
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture(null, null, null, TimeSpan.FromSeconds(5));
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;
            var token = new CancellationTokenSource();

            var agvGraph = CreateTestAgvGraph();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvSaga = new AgvSaga(
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

            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            token.CancelAfter(10);
            (await fixture.TestHarness.Published.Any<RequestAgvGraph>(token.Token)).ShouldBeFalse();
            (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
        }
        
        // ReSharper disable MethodSupportsCancellation
        /// <summary>
        /// Checks if only RequestAgvGraph is sent because AGV already has data
        /// </summary>
        [TestMethod]
        public async Task When_DataIsGivenAndGraphMissing_OnlyAgvGraphRequested()
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture(null, null, null, TimeSpan.FromSeconds(5));
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;
            var token = new CancellationTokenSource();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvData = new AgvData(correlationId, "Karis", "BF 2", null, 80, 1.2, 0.6, 0.8, 0.7, 0.9, 2.5, 1000, Array.Empty<AgvData.Sensor>());
            agvStateMachine.AgvDataRegistry.Object.AddOrUpdate(agvData);

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            {
                // Repeat request exceeded but no daily request
                AgvGraphRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, fixture.Machine.AgvRequestTimeoutS + 1),
                AgvDataRequestedLastTime = DateTime.UtcNow - new TimeSpan(0, 0, fixture.Machine.AgvRequestTimeoutS + 1)
            };

            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            token.CancelAfter(10);
            (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<RequestAgvData>(token.Token)).ShouldBeFalse();
        }

        [TestMethod]
        public async Task When_AgvBatteryChargeChange_Send_AgvBatteryChargeChanged()
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvSaga = new AgvSaga(
                    correlationId,
                    agvId,
                    fixture.Machine.Idle.Name,
                    new AgvStatus(correlationId, null, true, true, 10, true, true)
                );

            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
               new AgvStatus(correlationId, null, true, true, 11, true, true) // +1% battery charge
            ));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            (await fixture.TestHarness.Published.Any<AgvBatteryChargeChanged>()).ShouldBeTrue();
        }

        /// <summary>
        /// Checks if requests are repeated once a day
        /// </summary>
        [TestMethod]
        public async Task When_GraphAndDataReceivedOneDayAgo_RequestBothAgain()
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            var agvGraph = CreateTestAgvGraph();

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Discovered.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true),
                agvGraph: agvGraph
            )
            {
                AgvGraphRequestedLastTime = DateTime.UtcNow - new TimeSpan(24, 0, 1),
                AgvDataRequestedLastTime = DateTime.UtcNow - new TimeSpan(24, 0, 1)
            };

            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
        }

        /// <summary>
        /// Checks if requests are sent in the initial state
        /// </summary>
        [TestMethod]
        public async Task When_AgvInInitialState_RequestIsDoneUnconditionally()
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV_01";
            var correlationId = agvId.ToGuid();

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                currentState: fixture.Machine.Initial.Name,
                agvStatus: new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            {
                AgvGraphRequestedLastTime = DateTime.UtcNow,
                AgvDataRequestedLastTime = DateTime.UtcNow
            };

            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(
                correlationId,
                agvId,
                new AgvStatus(correlationId, null, false, false, 50, true, false)
            ));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvStatusReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();

            (await fixture.SagaHarness.Exists(correlationId)).ShouldBe(correlationId);
            (await fixture.TestHarness.Published.Any<RequestAgvData>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<RequestAgvGraph>()).ShouldBeTrue();
        }

        /// <summary>
        /// Checks if RequestAgvMap is not sent as soon as the corresponding RelatedMapId is found in the registry
        /// </summary>
        [DataRow(nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Busy))]
        [DataRow(nameof(AgvStateMachine.Charging))]
        [TestMethod]
        public async Task RequestAgvMap_NotSent_when_RelatedMapName_found_in_Registry(string previousState)
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            var agvSaga = CreateTestAgvSaga(previousState);    // The previous state is variable due to DuringAny()
            fixture.Add(agvSaga);

            var agvGraph = CreateTestAgvGraph();

            agvStateMachine.MockMapRegistry.Setup(mo => mo.Exists(agvGraph.RelatedMapId)).Returns(true);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvGraphReceived(agvSaga.CorrelationId, agvSaga.AgvId, agvGraph));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvGraphReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvGraphReceived>()).ShouldBeTrue();
            (await fixture.TestHarness.Published.Any<RequestAgvMap>()).ShouldBeFalse();
        }

        /// <summary>
        /// Checks if RequestAgvMap is received assuming that the registry is empty
        /// </summary>
        /// <remarks>When there is no match, a registry insertion must be invoked.</remarks>
        [DataRow(nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Busy))]
        [DataRow(nameof(AgvStateMachine.Charging))]

        [TestMethod]
        public async Task AgvMapReceived_is_Consumed_and_Processed_with_EmptyRegistry(string previousState)
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            var agvSaga = CreateTestAgvSaga(previousState);    // The previous state is variable due to DuringAny()
            fixture.Add(agvSaga);

            var agvMap = CreateTestAgvMap();
            agvStateMachine.MockMapRegistry.Setup(mo => mo.AddOrUpdate(agvMap.CorrelationId, agvMap.MapId, agvMap.ImageData, agvMap.Resolution, agvMap.XOffset, agvMap.YOffset, agvMap.ModificationDate)).Returns(true);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvMapReceived(agvSaga.CorrelationId, agvSaga.AgvId, agvMap));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvMapReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvMapReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Exists(agvSaga.CorrelationId)).ShouldBe(agvSaga.CorrelationId);

            agvStateMachine.MockMapRegistry.Verify(mo => mo.AddOrUpdate(agvMap.CorrelationId, agvMap.MapId, agvMap.ImageData, agvMap.Resolution, agvMap.XOffset, agvMap.YOffset, agvMap.ModificationDate), Times.Once);
        }

        /// <summary>
        /// Checks if RequestAgvMap is received assuming that the registry already contains a matching entry
        /// </summary>
        /// <remarks>When there is a match, any registry insertion must not happen.</remarks>
        [TestMethod]
        [DataRow(nameof(AgvStateMachine.Discovered))]
        [DataRow(nameof(AgvStateMachine.Idle))]
        [DataRow(nameof(AgvStateMachine.Busy))]
        [DataRow(nameof(AgvStateMachine.Charging))]
        public async Task AgvMapReceived_is_Consumed_and_Processed_with_Registry_containing_entries(string previousState)
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            var agvSaga = CreateTestAgvSaga(previousState);    // The previous state is variable due to DuringAny()
            fixture.Add(agvSaga);

            var agvMap = CreateTestAgvMap();
            agvStateMachine.MockMapRegistry.Setup(mo => mo.Exists(agvMap.MapId.ToGuid())).Returns(true);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvMapReceived(agvSaga.CorrelationId, agvSaga.AgvId, agvMap));

            // Assert
            (await fixture.TestHarness.Published.Any<AgvMapReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Consumed.Any<AgvMapReceived>()).ShouldBeTrue();
            (await fixture.SagaHarness.Exists(agvSaga.CorrelationId)).ShouldBe(agvSaga.CorrelationId);

            agvStateMachine.MockMapRegistry.Verify(mo => mo.AddOrUpdate(agvMap.CorrelationId, agvMap.MapId, agvMap.ImageData, agvMap.Resolution, agvMap.XOffset, agvMap.YOffset, agvMap.ModificationDate), Times.Once);
        }

        /// <summary>
        /// Checks if <see cref="AgvStartedCharging"/> has been consumed with required state transition to <seealso cref="AgvStateMachine.Charging"/>
        /// </summary>
        [DataRow(nameof(AgvStateMachine.Idle))]
        [TestMethod]
        public async Task AgvStartedCharging_Consumed(string previousState)
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            var agvSaga = CreateTestAgvSaga(previousState);
            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStartedCharging(agvSaga.CorrelationId));

            // Assert
            bool consumed = await fixture.SagaHarness.Consumed.Any<AgvStartedCharging>();
            consumed.ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == agvSaga.CorrelationId).First().Saga;
            saga.CurrentState.ShouldBe(nameof(AgvStateMachine.Charging));
        }

        /// <summary>
        /// Checks if <see cref="AgvStoppedCharging"/> has been consumed with required state transition to <seealso cref="AgvStateMachine.Idle"/>
        /// </summary>
        [TestMethod]
        public async Task AgvStoppedCharging_Consumed()
        {
            // Arrange
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            var agvSaga = CreateTestAgvSaga(nameof(AgvStateMachine.Charging));
            fixture.Add(agvSaga);

            // Act
            await fixture.TestHarness.Bus.Publish(new AgvStoppedCharging(agvSaga.CorrelationId));

            // Assert
            bool consumed = await fixture.SagaHarness.Consumed.Any<AgvStoppedCharging>();
            consumed.ShouldBeTrue();

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == agvSaga.CorrelationId).First().Saga;
            saga.CurrentState.ShouldBe(nameof(AgvStateMachine.Idle));
        }

        /// <summary>
        /// When AgvSaga state has changed, check if observer was triggered
        /// </summary>
        [TestMethod]
        public async Task When_AgvSaga_State_Changed_Trigger_Observer()
        {
            var agvStateMachine = new AgvStateMachineTestFixture();
            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var stateBefore = fixture.Machine.Idle;
            var stateAfter = fixture.Machine.Busy;

            var agvSaga = new AgvSaga(
                correlationId,
                agvId,
                stateBefore.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true, "R1"),
                new AgvGraph(correlationId, Array.Empty<AgvGraph.Node>(), Array.Empty<AgvGraph.Edge>(), Guid.Empty, "EmptyMap")
            );

            fixture.Add(agvSaga);

            IRequestClient<SetAgvBusy>? client = fixture.TestHarness.GetRequestClient<SetAgvBusy>();
            Response response = await client.GetResponse<SetAgvBusySucceeded, SetAgvBusyRejected>(
                new SetAgvBusy(correlationId));
            response.Message.ShouldBeAssignableTo(typeof(SetAgvBusySucceeded));

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.CurrentState.ShouldBe(stateAfter.Name);

            agvStateMachine.MockObserver.Verify(s => s.StateChanged(It.IsAny<BehaviorContext<AgvSaga>>(), stateAfter, stateBefore), Times.Once);
        }

        /// <summary>
        /// When AgvInitializedMessage is received, AgvOfflineWatchdog is started
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task When_AgvInitializedMessage_AgvOfflineWatchdogIsScheduled(bool agvInitialAlive)
        {
            var agvStateMachine = new AgvStateMachineTestFixture(
                x => x.AddDelayedMessageScheduler(),
                (_, cfg) => cfg.UseDelayedMessageScheduler());

            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var agv = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            { IsOnline = agvInitialAlive };
            fixture.Add(agv);

            await fixture.TestHarness.Bus.Publish(new AgvStateMachine.AgvInitializeMessage(correlationId, agvId));
            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            // Double check for initial alive (event when saga taken from the state machine)
            agv.IsOnline.ShouldBe(agvInitialAlive);

            (await fixture.SagaHarness.Consumed.Any<AgvStateMachine.AgvInitializeMessage>()).ShouldBeTrue();
            fixture.TestHarness.Sent.Select<AgvOfflineMessage>().ShouldNotBeEmpty();
        }

        /// <summary>
        /// When watchdog expires, AGV goes offline
        /// </summary>
        [TestMethod]
        public async Task When_AgvWatchdogExpires_AgvWentOffline()
        {
            const int agvOfflineTimeoutMsWhileIdle = 100;
            const int agvOfflineTimeoutMsWhileBusy = 500;

            var mockHub = new Mock<Core.Hubs.IAgvHubContext>();
            var agvStateMachine = new AgvStateMachineTestFixture(
                x =>
                {
                    x.AddDelayedMessageScheduler();
                    x.AddConsumer<AgvPropertyChangedConsumer>();
                    x.AddTransient(_ => mockHub.Object);
                },
                (_, cfg) => cfg.UseDelayedMessageScheduler(),
                new AgvSettings { AgvOfflineTimeoutMsWhileIdle = agvOfflineTimeoutMsWhileIdle, AgvOfflineTimeoutMsWhileBusy = agvOfflineTimeoutMsWhileBusy },
                TimeSpan.FromMilliseconds(2 * Math.Max(agvOfflineTimeoutMsWhileIdle, agvOfflineTimeoutMsWhileBusy)));

            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var agv = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, null, true, true, 10, true, true)
            )
            { IsOnline = true };
            fixture.Add(agv);

            await fixture.TestHarness.Bus.Publish(new AgvStateMachine.AgvInitializeMessage(correlationId, agvId));

            (await fixture.SagaHarness.Consumed.Any<AgvStateMachine.AgvInitializeMessage>()).ShouldBeTrue();
            fixture.TestHarness.Sent.Select<AgvOfflineMessage>().ShouldNotBeEmpty();
            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            agv.IsOnline.ShouldBeTrue();

            (await fixture.SagaHarness.Consumed.Any<AgvOfflineMessage>()).ShouldBeTrue();
            var now = DateTime.UtcNow;
            var msg = fixture.SagaHarness.Consumed.Select<AgvOfflineMessage>().First().Context.Message;
            // When triggered initially by AgvInitializeMessage, the timeout for busy state is taken into account
            ((now - msg.Created).TotalMilliseconds > 0.9 * agvOfflineTimeoutMsWhileBusy).ShouldBeTrue();
            ((now - msg.Created).TotalMilliseconds < 1.5 * agvOfflineTimeoutMsWhileBusy).ShouldBeTrue();

            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            agv.IsOnline.ShouldBeFalse();

            (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
            (await fixture.TestHarness.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();
            var propertyChangedMessage = fixture.TestHarness.Consumed.Select<AgvPropertyChanged>().First().Context.Message;
            propertyChangedMessage.Agv.IsOnline.ShouldBeFalse();
        }

        [TestMethod]
        public async Task When_AlternateGameBetweenWatchdogAndStatus_AgvTogglesOfflineAndOnline()
        {
            const int agvOfflineTimeoutMsWhileIdle = 500;
            const int agvOfflineTimeoutMsWhileBusy = 1000;

            var mockHub = new Mock<Core.Hubs.IAgvHubContext>();
            var agvStateMachine = new AgvStateMachineTestFixture(
                x =>
                {
                    x.AddDelayedMessageScheduler();
                    x.AddConsumer<AgvPropertyChangedConsumer>();
                    x.AddTransient(_ => mockHub.Object);
                },
                (_, cfg) => cfg.UseDelayedMessageScheduler(),
                new AgvSettings { AgvOfflineTimeoutMsWhileIdle = agvOfflineTimeoutMsWhileIdle, AgvOfflineTimeoutMsWhileBusy = agvOfflineTimeoutMsWhileBusy },
                TimeSpan.FromMilliseconds(2 * Math.Max(agvOfflineTimeoutMsWhileIdle, agvOfflineTimeoutMsWhileBusy)));

            StateMachineTestFixture<AgvStateMachine, AgvSaga> fixture = agvStateMachine.Fixture;

            const string agvId = "AGV#1";
            var correlationId = agvId.ToGuid();
            var agv = new AgvSaga(
                correlationId,
                agvId,
                fixture.Machine.Idle.Name,
                new AgvStatus(correlationId, "first", false, true, 60, false, false)
            )
            { IsOnline = true };
            fixture.Add(agv);

            var agvStatus1 = new AgvStatus(correlationId, "first", false, true, 60, false, false);
            var agvStatus2 = new AgvStatus(correlationId, "second", false, true, 60, false, false);

            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus1));

            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>()).ShouldBeTrue();
            fixture.TestHarness.Sent.Select<AgvOfflineMessage>().ShouldNotBeEmpty();

            (await fixture.SagaHarness.Consumed.Any<AgvOfflineMessage>()).ShouldBeTrue();
            var now = DateTime.UtcNow;
            var msg = fixture.SagaHarness.Consumed.Select<AgvOfflineMessage>().First().Context.Message;
            // When triggered by AgvStatusReceived, the timeout depends on the state (and this AGV is idle)
            ((now - msg.Created).TotalMilliseconds > 0.9 * agvOfflineTimeoutMsWhileIdle).ShouldBeTrue();
            ((now - msg.Created).TotalMilliseconds < 1.5 * agvOfflineTimeoutMsWhileIdle).ShouldBeTrue();

            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            agv.IsOnline.ShouldBeFalse();

            // Make sure change has been published
            (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
            (await fixture.TestHarness.Consumed.Any<AgvPropertyChanged>()).ShouldBeTrue();
            var propertyChangedMessage = fixture.TestHarness.Consumed.Select<AgvPropertyChanged>().First().Context.Message;
            propertyChangedMessage.Agv.IsOnline.ShouldBeFalse();

            // Make sure nobody did spoil the test
            agvStatus1.OrderId.ShouldNotBe(agvStatus2.OrderId);

            await fixture.TestHarness.Bus.Publish(new AgvStatusReceived(correlationId, agvId, agvStatus2));
            // We need to wait for the "second" message (don't check just for any because a first message is already there)
            (await fixture.SagaHarness.Consumed.Any<AgvStatusReceived>(x => x.Context.Message.AgvStatus.OrderId == "second")).ShouldBeTrue();

            agv = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            agv.IsOnline.ShouldBeTrue();

            // Make sure second change has been published
            (await fixture.TestHarness.Published.Any<AgvPropertyChanged>()).ShouldBeTrue();
            // Ignore first message with alive equals true
            (await fixture.TestHarness.Consumed.Any<AgvPropertyChanged>(x => x.Context.Message.Agv.IsOnline == false)).ShouldBeTrue();
        }
    }
}