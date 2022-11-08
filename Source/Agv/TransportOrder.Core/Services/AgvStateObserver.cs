using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Observer for <see cref="AgvSaga"/>
    /// </summary>
    public class AgvStateObserver : IStateObserver<AgvSaga>
    {
        private readonly IAgvHubContext _agvHubContext;

        /// <summary>
        /// Initializes a new instance of <see cref="AgvStateObserver"/>
        /// </summary>
        public AgvStateObserver(IAgvHubContext agvHubContext)
        {
            _agvHubContext = agvHubContext;
        }

        /// <summary>
        /// Handle state change of an AgvSaga
        /// </summary>
        public async Task StateChanged(BehaviorContext<AgvSaga> context, State currentState, State previousState)
        {
            if (Equals(currentState, context.StateMachine.Initial))
            {
                await _agvHubContext.SendAgvStateToAllClients(nameof(Defines.HubMethods.AgvDiscovered), context.Saga, context.CancellationToken);
            }
            else
            {
                await _agvHubContext.SendAgvStateToAllClients(nameof(Defines.HubMethods.AgvStateChanged), context.Saga, context.CancellationToken);
            }
        }
    }
}
