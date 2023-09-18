using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Responsible for choosing the best Agv for an ActionGroup and also to choose the best ActionGroup for an Agv
/// </summary>
public interface IChooseAgvActionGroupService
{
    /// <summary>
    /// Tries to find a suitable AGV for this ActionGroup
    /// Find AGV with shortest route to pick
    /// <param name="actionGroup"></param>
    /// <param name="agvs">The Agvs to consider</param>
    /// <param name="cancellationToken"></param>
    /// </summary>
    Task<IAgv?> GetAgvAsync(Models.ActionGroup.ActionGroup actionGroup, IList<IAgv> agvs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get not started ActionGroups, returns the best action in the first ActionGroup that Agv can handle all actions
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(IAgv agv, CancellationToken cancellationToken = default);
}