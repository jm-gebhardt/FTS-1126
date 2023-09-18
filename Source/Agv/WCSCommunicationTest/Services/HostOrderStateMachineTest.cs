using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services;

[TestClass]
public class WcsCommunicatorTest
{
    private readonly TimeSpan _testInactivity = TimeSpan.FromSeconds(3);

    private HostOrderSaga CreateHostOrderSaga(string sagaState)
    {
        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);
        return new HostOrderSaga(orderDetails, sagaState);
    }

    /// <summary>
    /// When HostOrder received agv Is requested
    /// </summary>
    [TestMethod]
    public async Task WhenCreateHostOrderReceived_HostOrderSagaCreated()
    {
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);

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
    public async Task WhenCreateHostOrderReceived_ActionGroup_Sent()
    {
        // Arrange
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        // Act
        await fixture.TestHarness.Bus.Publish(new CreateHostOrder(orderDetails));

        // Assert
        (await fixture.SagaHarness.Consumed.Any<CreateHostOrder>()).ShouldBeTrue();

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


    [TestMethod]
    public async Task Consumed_ActionGroupStarted_InStateInitial_TransitionToStateOrderQueued()
    {
        var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.Initial));
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);
        fixture.Add(saga);
        var actionGroup = saga.GetPickDropActionGroup();

        await fixture.TestHarness.Bus.Publish(new ActionGroupCreated(actionGroup, actionGroup.CorrelationId));
        (await fixture.SagaHarness.Consumed.Any<ActionGroupCreated>()).ShouldBeTrue();

        saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.OrderQueued));
    }

    [TestMethod]
    public async Task Consumed_ActionGroupStarted_InStateOrderQueued_HostOrderStartedPublishedInStateStarted()
    {
        var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.OrderQueued));
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);
        fixture.Add(saga);
        var actionGroup = saga.GetPickDropActionGroup();
        actionGroup.AssignedAgv = "Agv".ToGuid();

        await fixture.TestHarness.Bus.Publish(new ActionGroupStarted(actionGroup, actionGroup.CorrelationId));

        (await fixture.SagaHarness.Consumed.Any<ActionGroupStarted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HostOrderStarted>()).ShouldBeTrue();
        saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Started));
        saga.AssignedAgv.ShouldNotBeNull();
    }


    [TestMethod]
    public async Task Handle_PickCompletedEvent()
    {
        var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.Started));
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);
        fixture.Add(saga);

        await fixture.TestHarness.Bus.Publish(new ActionCompletedEvent(saga.PickActionGuid));

        (await fixture.SagaHarness.Consumed.Any<ActionCompletedEvent>()).ShouldBeTrue();
        saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Picked));
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_InStatePicked_PublishesHostOrderHandlingSucceededInStateComplete()
    {
        var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.Picked));
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);
        fixture.Add(saga);
        var actionGroup = saga.GetPickDropActionGroup();
        actionGroup.AssignedAgv = "Agv".ToGuid();

        await fixture.TestHarness.Bus.Publish(new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId));
        (await fixture.SagaHarness.Consumed.Any<ActionGroupCompleted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HostOrderHandlingSucceeded>()).ShouldBeTrue();

        saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Completed));
    }

    [TestMethod]
    public async Task Consume_ActionCompleted_InStateStarted_TransitionToStatePicked()
    {
        var saga = CreateHostOrderSaga(nameof(HostOrderStateMachine.Started));
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);
        fixture.Add(saga);
        var actionGroup = saga.GetPickDropActionGroup();
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        actionGroup.AssignedAgv = "Agv".ToGuid();

        await fixture.TestHarness.Bus.Publish(new ActionCompletedEvent(pickAction.CorrelationId));
        (await fixture.SagaHarness.Consumed.Any<ActionCompletedEvent>()).ShouldBeTrue();

        saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Picked));
    }

    [TestMethod]
    [DataRow(nameof(HostOrderStateMachine.OrderQueued))]
    [DataRow(nameof(HostOrderStateMachine.Started))]
    [DataRow(nameof(HostOrderStateMachine.Picked))]
    [DataRow(nameof(HostOrderStateMachine.Invalid))]
    public async Task Consume_ActionGroupCancelled_InValidState_PublishesHostOrderCancelSucceededInStateCancelled(string validState)
    {
        var saga = CreateHostOrderSaga(validState);
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);
        fixture.Add(saga);
        var actionGroup = saga.GetPickDropActionGroup();

        await fixture.TestHarness.Bus.Publish(new ActionGroupCancelled(actionGroup, actionGroup.CorrelationId));
        (await fixture.SagaHarness.Consumed.Any<ActionGroupCancelled>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HostOrderCancelSucceeded>()).ShouldBeTrue();

        saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Cancelled));
    }


    [TestMethod]
    [DataRow(nameof(HostOrderStateMachine.OrderQueued))]
    [DataRow(nameof(HostOrderStateMachine.Started))]
    [DataRow(nameof(HostOrderStateMachine.Picked))]
    [DataRow(nameof(HostOrderStateMachine.Invalid))]
    public async Task Consume_ActionGroupCancelFailed_InValidState_PublishesHostOrderCancelFailedInStateCancelFailed(string validState)
    {
        var saga = CreateHostOrderSaga(validState);
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);
        fixture.Add(saga);
        var actionGroup = saga.GetPickDropActionGroup();
        const HostOrderCancelFailedErrorCode error = HostOrderCancelFailedErrorCode.OrderAlreadyStarted;
        await fixture.TestHarness.Bus.Publish(new ActionGroupCancelFailed(actionGroup, actionGroup.CorrelationId, error));
        (await fixture.SagaHarness.Consumed.Any<ActionGroupCancelFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HostOrderCancelFailed>()).ShouldBeTrue();

        saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.CancelFailed));
    }

    [TestMethod]
    [DataRow(nameof(HostOrderStateMachine.OrderQueued))]
    [DataRow(nameof(HostOrderStateMachine.Started))]
    [DataRow(nameof(HostOrderStateMachine.Picked))]
    [DataRow(nameof(HostOrderStateMachine.Invalid))]
    public async Task Consume_ActionGroupFailed_InValidState_PublishesHostOrderHandlingFailedInStateFailed(string validState)
    {
        var saga = CreateHostOrderSaga(validState);
        var fixture = new HostOrderStateMachineTestFixture(inactivityTimeout: _testInactivity);
        fixture.Add(saga);
        var actionGroup = saga.GetPickDropActionGroup();
        const HostOrderHandlingFailedErrorCode error = HostOrderHandlingFailedErrorCode.FailedByTransportLayer;
        await fixture.TestHarness.Bus.Publish(new ActionGroupFailed(actionGroup, actionGroup.CorrelationId, error));
        (await fixture.SagaHarness.Consumed.Any<ActionGroupFailed>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<HostOrderHandlingFailed>()).ShouldBeTrue();

        saga.CurrentState.ShouldBe(nameof(HostOrderStateMachine.Failed));
    }
}