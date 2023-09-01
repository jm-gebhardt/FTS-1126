using MassTransit;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Messages.Events.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;

/// <summary>
/// Observer for <see cref="AgvSaga"/>
/// </summary>
public class AgvStateObserver : IStateObserver<AgvSaga>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AgvStateObserver"/>
    /// </summary>
    public AgvStateObserver()
    {
    }

    /// <summary>
    /// Handle state change of an AgvSaga
    /// </summary>
    public async Task StateChanged(BehaviorContext<AgvSaga> context, State currentState, State? previousState)
    {
        await context.Publish(new AgvStateChanged(context.Saga.CorrelationId, context.Saga, previousState?.Name));
    }
}