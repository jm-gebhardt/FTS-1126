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
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(busRegistrationConfigurator: x =>
            {
                x.AddRequestClient<ReserveAgv>();
            });

            const string agvId = "Order#1";
            var correlationId = agvId.ToGuid();
            var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);
            var agvStage = new AgvSaga(correlationId, agvId, fixture.Machine.Idle.Name, agvStatus);

            fixture.Add(agvStage);

            IRequestClient<ReserveAgv>? client = fixture.TestHarness.GetRequestClient<ReserveAgv>();
            Response response = await client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(new ReserveAgv(correlationId));
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
            var fixture = new StateMachineTestFixture<AgvStateMachine, AgvSaga>(busRegistrationConfigurator: x =>
            {
                x.AddRequestClient<ReserveAgv>();
            });

            const string agvId = "Order#1";
            var correlationId = agvId.ToGuid();
            var agvStatus = new AgvStatus(correlationId, null, true, true, 10, true, true);
            var agvStage = new AgvSaga(correlationId, agvId, acceptedStates, agvStatus);

            fixture.Add(agvStage);

            IRequestClient<ReserveAgv>? client = fixture.TestHarness.GetRequestClient<ReserveAgv>();
            Response response = await client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(new ReserveAgv(correlationId));

            Assert.IsInstanceOfType(response.Message, typeof(AgvReservationRejected));

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            Assert.AreEqual(acceptedStates, saga.CurrentState);
        }
    }
}
