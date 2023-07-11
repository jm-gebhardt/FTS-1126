using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs
{
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
}
