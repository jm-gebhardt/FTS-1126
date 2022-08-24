using MassTransit;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Observer for AgvSaga
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
            await _agvHubContext.SendAgvToAllClients("AgvStateChanged", context.Saga, context.CancellationToken);
        }
    }
}
