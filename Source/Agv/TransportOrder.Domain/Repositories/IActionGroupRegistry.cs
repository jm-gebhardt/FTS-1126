using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

// ReSharper disable UnusedMemberInSuper.Global
namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Interface for ActionGroup and AgvAction registry
/// </summary>
public interface IActionGroupRegistry
{
    /// <summary>
    /// Saves the action
    /// </summary>
    /// <param name="actionGroup">Agv Action</param>
    Task AddAsync(ActionGroup actionGroup);

    /// <summary>
    /// Get ActionGroup
    /// </summary>
    /// <param name="states">Can be one state filter or contain multiple states which should be included in the return. If null, the filter is inactive</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered</param>
    /// <param name="toDate">The UTC from Date, only items with a less CreatedDate will be considered</param>
    /// <param name="assignedAgvCorrelationId">Correlation Id of assigned Agv</param>
    /// <param name="maxItems">The max item count to return</param>
    /// <param name="isManual">Action Group is triggered manually not automatic by the system</param>
    /// <returns></returns>
    Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(IList<ActionGroupState>? states = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0, Guid? assignedAgvCorrelationId = null, bool? isManual = null);

    /// <summary>
    /// Get ActionGroup
    /// </summary>
    /// <param name="state">One state filter which should be included in the return.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered</param>
    /// <param name="toDate">The UTC from Date, only items with a less CreatedDate will be considered</param>
    /// <param name="assignedAgvCorrelationId">Correlation Id of assigned Agv</param>
    /// <param name="maxItems">The max item count to return</param>
    /// <param name="isManual">Action Group was created manually or automatic. If null, the filter not considered</param>
    /// <returns></returns>
    Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(ActionGroupState state, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0, Guid? assignedAgvCorrelationId = null, bool? isManual = null);

    /// <summary>
    /// Get ActionGroup count which have the given state (and date range filter)
    /// </summary>
    /// <param name="states">Can be one state filter or contain multiple states which should be included in the return</param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <returns></returns>
    Task<int> GetActionGroupsCountAsync(IEnumerable<ActionGroupState> states, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get current AgvAction of provided AGV
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns>The current (running) AgvAction of the current ActionGroup</returns>
    Task<AgvAction?> GetCurrentAgvActionAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get currently assigned action groups of provided AGV
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns>The most recent ActionGroup of the Agv. It may already be completed.</returns>
    Task<ActionGroup?> GetCurrentAssignedActionGroupAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get currently assigned action groups of provided AGVs
    /// </summary>
    /// <param name="agvCorrelationIds"></param>
    /// <returns>The most recent ActionGroup for each Agv. They may already be completed.</returns>
    Task<IReadOnlyDictionary<Guid, ActionGroup?>> GetCurrentAssignedActionGroupsAsync(IEnumerable<Guid> agvCorrelationIds);

    /// <summary>
    /// Get ActionGroup
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    Task<ActionGroup?> GetActionGroupAsync(Guid correlationId);

    /// <summary>
    /// Get ActionGroup
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    Task<ActionGroup> GetRequiredActionGroupByIdAsync(Guid correlationId);

    /// <summary>
    /// Get Agv action by key.
    /// </summary>
    /// <param name="correlationId">Unique ID per action</param>
    /// <returns>Returns Agv action identified by key or null when not found</returns>
    Task<AgvAction?> GetActionByIdAsync(Guid correlationId);

    /// <summary>
    /// Get Agv action by key.
    /// </summary>
    /// <param name="correlationId">Unique ID per action</param>
    /// <returns>Returns Agv action identified by key or throws when not found</returns>
    Task<AgvAction> GetRequiredActionByIdAsync(Guid correlationId);

    /// <summary>
    /// Delete all actions with the same parent id
    /// </summary>
    /// <param name="parentId">Correlation id of the parent object</param>
    Task DeleteByParentIdAsync(Guid parentId);

    /// <summary>
    /// Updates the Action State/Date of the existing ActionGroup and also the ActionGroup State/Date
    /// </summary>
    /// <param name="action">The action to be started</param>
    /// <param name="agv">The assigned Agv</param>
    /// <param name="state">The new state</param>
    Task<ActionGroup> UpdateActionAndGroupStateAsync(AgvAction action, IAgv agv, ActionState state);

    /// <summary>
    /// Determine if there is any drop action which is not completed (Agv loaded)
    /// </summary>
    /// <param name="agvCorrelationId">The AgvCorrelationId of the agv to check</param>
    /// <returns>True if there is any not completed drop action for the given Agv</returns>
    Task<bool> HasUnfinishedDropActionsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get ActionGroups where no action was started yet
    /// </summary>
    /// <param name="agvCorrelationId">The agvCorrelationId to filter for</param>
    Task<IReadOnlyList<ActionGroup>> GetNotStartedActionGroupsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get ActionGroups where one or more actions are invalid
    /// </summary>
    Task<IReadOnlyList<ActionGroup>> GetInvalidActionGroupsAsync();

    /// <summary>
    /// Return the subsequent action for the Agv that is not started yet.
    /// So that's normally the drop action after the completion of the preceding pick action
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns>Returns subsequent action</returns>
    Task<AgvAction> GetSubsequentActionAsync(Guid agvCorrelationId);

    /// <summary>
    /// Checks if the agv has any assigned action groups or new ones that are unassigned.
    /// </summary>
    /// <param name="agvCorrelationId">The id of the agv.</param>
    Task<bool> AgvHasPendingActions(Guid agvCorrelationId);

    /// <summary>
    /// Checks if the agv is currently heading to a node for and action of a particular type.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="nodeType">The type of the action.</param>
    Task<bool> IsAgvHeadingToNodeForActionOfType(Guid agvCorrelationId, AutomaticActionOrderId nodeType);
}