using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services;

/// <summary>
/// Provides an observer for the <see cref="HostOrderSaga"/>.
/// </summary>
public class HostOrderStateObserver : IStateObserver<HostOrderSaga>
{
    /// <summary>
    /// Handle state change of an OrderSaga
    /// </summary>
    public async Task StateChanged(BehaviorContext<HostOrderSaga> context, State currentState, State previousState)
    {
        if (context.CorrelationId == null)
        {
            return;
        }

        var correlationId = context.CorrelationId.Value;

        switch (currentState.Name)
        {
            case nameof(HostOrderStateMachine.Started):
                await context.Publish(new HostOrderStarted(correlationId, context.Saga.OrderId, context.Saga.AssignedAgv!));
                break;

            case nameof(HostOrderStateMachine.Completed):
                await context.Publish(new HostOrderHandlingSucceeded(correlationId, context.Saga.OrderId));
                break;

            case nameof(HostOrderStateMachine.Failed):
                await context.Publish(new HostOrderHandlingFailed(correlationId, context.Saga.OrderId, context.Saga.HandlingError));
                break;

            case nameof(HostOrderStateMachine.Cancelled):
                await context.Publish(new HostOrderCancelSucceeded(correlationId, context.Saga.OrderId));
                break;

            case nameof(HostOrderStateMachine.CancelFailed):
                await context.Publish(new HostOrderCancelFailed(correlationId, context.Saga.OrderId, context.Saga.CancellationError));
                break;

            case nameof(HostOrderStateMachine.Invalid):
                await context.Publish(new HostOrderHandlingFailed(correlationId, context.Saga.OrderId, HostOrderHandlingFailedErrorCode.InvalidOrderData));
                break;
        }
    }
}