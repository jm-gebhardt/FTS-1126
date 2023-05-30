using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;

/// <summary>
/// Responsible for choosing the best Agv for an ActionGroup and also to choose the best ActionGroup for an Agv
/// </summary>
public interface IChooseAgvActionGroupService
{
    /// <summary>
    /// Tries to find a suitable AGV for this ActionGroup
    /// Find AGV with shortest route to pick
    /// </summary>
    Task<IAgv?> GetAgvAsync(ActionGroup actionGroup, IList<IAgv> agvs);

    /// <summary>
    /// Get not started ActionGroups, returns the best action in the first ActionGroup that Agv can handle all actions
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    Task<AgvAction?> GetActionGroupAsync(IAgv agv);
}