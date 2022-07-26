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
            var correlationId = Guid.Parse(AgvGraphBuilderStateMachine.InternalTestingRequestId);

            var fixture = new StateMachineTestFixture<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>();

            await fixture.TestHarness.Bus.Publish(new RequestAgvGraph(correlationId, AgvTestIdentifier01));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<RequestAgvGraph>());

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttRequestStationList>());
            Assert.AreEqual(fixture.SagaHarness.StateMachine.RequestAgvGraphReceived, saga.CurrentState);

            var stationList = new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow,
                new Station[] {
                    new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
                });
            await fixture.TestHarness.Bus.Publish(new StationListReceived(correlationId, stationList));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<StationListReceived>());

            Assert.IsNotNull(saga.MqttStationList);
            Assert.IsNull(saga.AgvGraph);
            Assert.AreEqual(fixture.SagaHarness.StateMachine.StationListReceived, saga.CurrentState);

            var graph = new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, 2, 1,
                new Node[] {
                    new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                    new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
                },
                new Edge[] {
                    new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
                });
            await fixture.TestHarness.Bus.Publish(new GraphReceived(correlationId, graph));
            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<StationListReceived>());

            var publishedData = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;
            Assert.AreEqual(correlationId, publishedData.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier01.ToGuid(), publishedData.Graph.CorrelationId);
            Assert.AreEqual(2 + 1, publishedData.Graph.Nodes.Length);
            Assert.AreEqual(2 + 2, publishedData.Graph.Edges.Length);
        }
    }
}
