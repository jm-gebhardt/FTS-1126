using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// Service for dealing with AgvAction and ActionGroups
/// </summary>
public interface IActionGroupService
{
    /// <summary>
    /// Returns the ActionGroup from the registry
    /// </summary>
    /// <param name="actionGroupCorrelationId"></param>
    /// <returns></returns>
    public ActionGroup GetActionGroup(Guid actionGroupCorrelationId);

    /// <summary>
    /// Updates the action of the existing ActionGroup and also the ActionGroup State
    /// </summary>
    /// <param name="action">The action to be started</param>
    /// <param name="agv">The assigned Agv</param>
    /// <param name="state">The new state</param>
    public Task UpdateActionAndGroupStateAsync(AgvAction action, IAgv agv, ActionState state);
}