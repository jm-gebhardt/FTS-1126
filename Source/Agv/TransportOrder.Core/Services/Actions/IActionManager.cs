using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions
{
    /// <summary>
    /// Action Manager
    /// </summary>
    public interface IActionManager
    {
        /// <summary>
        /// Save ActionGroup and try to assign it to an Agv
        /// </summary>
        /// <param name="actionGroup"></param>
        /// <param name="cancellationToken"></param>
        public Task AddActionGroupAsync(ActionGroup actionGroup, CancellationToken cancellationToken);

        /// <summary>
        /// Update ActionState and unlock Agv
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [UsedImplicitly]
        public Task CompleteActionAsync(Guid correlationId, CancellationToken cancellationToken);

        /// <summary>
        /// External trigger for assigning a new ActionGroup to an Agv
        /// </summary>
        /// <param name="agv"></param>
        /// <param name="ignoreChargingFlag">If true the return value will not take the charging flag of the Agv into account</param>
        /// <param name="cancellationToken"></param>
        public Task AssignExclusivelyAsync(IAgv agv, bool ignoreChargingFlag = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if it makes sense to assign a new action to any charging Agv
        /// </summary>
        public Task CheckChargingAgvs(CancellationToken cancellationToken);
    }
}