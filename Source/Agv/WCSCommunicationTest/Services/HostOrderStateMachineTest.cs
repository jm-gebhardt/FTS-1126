using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services
{
    [TestClass]
    public class HostOrderStateMachineTest
    {
        /// <summary>
        /// When HostOrder received agv Is requested
        /// </summary>
        [TestMethod]
        public async Task WhenCreateHostOrderReceived_HostOrderSagaCreated()
        {
            var fixture = new HostOrderStateMachineFixture();

            const string orderId = "Order#01";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.SendAndVerifyMessage(new CreateHostOrder(orderDetails));

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            saga.CurrentState.ShouldBe("Initial");
            saga.OrderDetails.CorrelationId.ShouldBe(correlationId);
            saga.OrderDetails.OrderId.ShouldBe(orderId);
        }

        [TestMethod]
        public async Task WhenActionGroupInvalidReceived_TransitionToFailed()
        {
            var fixture = new HostOrderStateMachineFixture();

            const string orderId = "Order#01";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);
            fixture.Add(new HostOrderSaga(orderDetails));
            var actionGroup = new ActionGroup(correlationId, string.Empty);
            await fixture.SendAndVerifyMessage(new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, new List<string>()));

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Failed));
        }

        [TestMethod]
        public async Task WhenCreateHostOrderReceived_ActionGroup_Sent()
        {
            // Arrange
            var fixture = new HostOrderStateMachineFixture();

            const string orderId = "Order#01";
            var correlationId = orderId.ToGuid();
            var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            // Act
            await fixture.SendAndVerifyMessage(new CreateHostOrder(orderDetails));

            // Assert
            (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();

            var msg = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
            msg.CorrelationId.ShouldBe(correlationId);
            var pickAction = msg.ActionGroup.Actions.First();
            var dropAction = msg.ActionGroup.Actions.Last();

            pickAction.ShouldNotBeNull();
            dropAction.ShouldNotBeNull();

            pickAction.ActionType.ShouldBe(ActionType.Pick);
            pickAction.ParentId.ShouldBe(correlationId);
            pickAction.CorrelationId.ShouldNotBe(Guid.Empty);

            dropAction.ActionType.ShouldBe(ActionType.Drop);
            dropAction.ParentId.ShouldBe(correlationId);
            dropAction.CorrelationId.ShouldNotBe(Guid.Empty);

            var saga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
            saga.ShouldNotBeNull();
            saga.PickActionGuid.ShouldBe(pickAction.CorrelationId);
            saga.DropActionGuid.ShouldBe(dropAction.CorrelationId);
        }
    }
}