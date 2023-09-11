using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services;

[TestClass]
public class HostOrderStateObserverTest
{
    private HostOrderSaga CreateHostOrderSaga()
    {
        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);
        return new HostOrderSaga(orderDetails, nameof(HostOrderStateMachine.Initial));
    }

    private async Task TestStateChange<TMessage>(string newState) where TMessage : class, CorrelatedBy<Guid>
    {
        var observer = new HostOrderStateObserver();
        var contextMock = new Mock<BehaviorContext<HostOrderSaga>>();
        var currentStateMock = new Mock<State>();
        var previousStateMock = new Mock<State>();
        var saga = CreateHostOrderSaga();

        contextMock.SetupGet(x => x.CorrelationId).Returns(Guid.Empty);
        contextMock.SetupGet(x => x.Saga).Returns(saga);
        currentStateMock.SetupGet(x => x.Name).Returns(newState);

        await observer.StateChanged(contextMock.Object, currentStateMock.Object, previousStateMock.Object);

        contextMock.Verify(x => x.Publish(It.IsAny<TMessage>(), default), Times.Once);
    }

    [TestMethod]
    public async Task TransitionWithoutCorrelationId_SendNoMessage()
    {
        var observer = new HostOrderStateObserver();
        var contextMock = new Mock<BehaviorContext<HostOrderSaga>>();
        var currentStateMock = new Mock<State>();
        var previousStateMock = new Mock<State>();

        Guid? correlationId = null;
        contextMock.SetupGet(x => x.CorrelationId).Returns(correlationId);

        await observer.StateChanged(contextMock.Object, currentStateMock.Object, previousStateMock.Object);

        contextMock.Verify(x => x.Publish(It.IsAny<CorrelatedBy<Guid>>(), default), Times.Never);
    }

    [TestMethod]
    public async Task TransitionToState_Started_SendMessage_HostOrderStartedEvent()
    {
        await TestStateChange<HostOrderStarted>(nameof(HostOrderStateMachine.Started));
    }

    [TestMethod]
    public async Task TransitionToState_Completed_SendMessage_HostOrderHandlingSucceededEvent()
    {
        await TestStateChange<HostOrderHandlingSucceeded>(nameof(HostOrderStateMachine.Completed));
    }

    [TestMethod]
    public async Task TransitionToState_Failed_SendMessage_HostOrderHandlingFailed()
    {
        await TestStateChange<HostOrderHandlingFailed>(nameof(HostOrderStateMachine.Failed));
    }

    [TestMethod]
    public async Task TransitionToState_Cancelled_SendMessage_HostOrderCancelSucceeded()
    {
        await TestStateChange<HostOrderCancelSucceeded>(nameof(HostOrderStateMachine.Cancelled));
    }

    [TestMethod]
    public async Task TransitionToState_CancelFailed_SendMessage_HostOrderCancelFailed()
    {
        await TestStateChange<HostOrderCancelFailed>(nameof(HostOrderStateMachine.CancelFailed));
    }

    [TestMethod]
    public async Task TransitionToState_Invalid_SendMessage_HostOrderHandlingFailed()
    {
        await TestStateChange<HostOrderHandlingFailed>(nameof(HostOrderStateMachine.Invalid));
    }
}