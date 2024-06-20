using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using JetBrains.Annotations;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

// ReSharper disable UnusedMemberInSuper.Global
namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Interface for ActionGroup and AgvAction repository.
/// </summary>
public interface IActionGroupRepository
{
    /// <summary>
    /// Saves the action
    /// </summary>
    /// <param name="actionGroup">Agv Action</param>
    Task AddAsync(ActionGroup actionGroup);

    /// <summary>
    /// Get a list of ActionGroup.
    /// </summary>
    /// <param name="states">Can be one actionGroupState filter or contain multiple states which should be included in the return. If null, the filter is inactive</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered</param>
    /// <param name="toDate">The UTC from Date, only items with a less CreatedDate will be considered</param>
    /// <param name="assignedAgvCorrelationId">Correlation Id of assigned Agv</param>
    /// <param name="maxItems">The max item count to return</param>
    /// <param name="isManual">Action Group is triggered manually not automatic by the system</param>
    /// <param name="sortBy">The sorting parameter.</param>
    /// <param name="sortDescending">If true, sorts in descending order.</param>
    /// <returns></returns>
    Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(IList<ActionGroupState>? states = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 100, Guid? assignedAgvCorrelationId = null, bool? isManual = null, SortBy sortBy = SortBy.CreatedDate, bool sortDescending = true);

    /// <summary>
    /// Get a list of ActionGroup.
    /// </summary>
    /// <param name="actionGroupState">One actionGroupState filter which should be included in the return parameter.</param>
    /// <param name="fromDate">The UTC from date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to date, only items with a less CreatedDate will be considered.</param>
    /// <param name="assignedAgvCorrelationId">The correlation id of assigned agv.</param>
    /// <param name="maxItems">The maximum number of items to return.</param>
    /// <param name="isManual">The action group was created manually or automatic. If null, the filter is not considered.</param>
    /// <returns></returns>
    Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(ActionGroupState actionGroupState, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0, Guid? assignedAgvCorrelationId = null, bool? isManual = null);

    /// <summary>
    /// Get the next manual action group for an agv.
    /// </summary>
    /// <param name="assignedAgv">The assigned agv.</param>
    /// <param name="allowedActionTypes">Optional list of action types to be contained in the group.</param>
    Task<ActionGroup?> GetNextManualActionGroupForAgvAsync(Guid assignedAgv, List<ActionType>? allowedActionTypes=null);

    /// <summary>
    /// Get the durations of actions in action groups that are in given actionGroupState and
    /// go from a source node to a target node. Both source and target are optional.
    /// </summary>
    /// <param name="actionGroupState">The actionGroupState of the action groups.</param>
    /// <param name="sourceNodeId">The source node id.</param>
    /// <param name="targetNodeId">The target node id.</param>
    /// <param name="maxItems">The maximum number of groups returned.</param>
    /// <returns></returns>
    Task<Dictionary<ActionType, ICollection<double>>> GetActionDurationsByTypeAsync(ActionGroupState actionGroupState, string? sourceNodeId=null, string? targetNodeId=null, int maxItems = 1000);

    /// <summary>
    /// Get ActionGroup count which have the given actionGroupState (and date range filter).
    /// </summary>
    /// <param name="states">Can be one actionGroupState filter or contain multiple states which should be included in the return</param>
    /// <param name="fromDate">The UTC from date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to date, only items with a less CreatedDate will be considered.</param>
    /// <returns></returns>
    Task<int> GetActionGroupsCountAsync(IEnumerable<ActionGroupState> states, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get the current AgvAction of provided agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <returns>The current (running) AgvAction of the current ActionGroup.</returns>
    Task<AgvAction?> GetCurrentAgvActionAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get currently assigned action groups of provided agv.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    /// <returns>The most recent ActionGroup of the agv. It may already be completed.</returns>
    [PublicAPI]
    Task<ActionGroup?> GetCurrentAssignedActionGroupAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get currently assigned action groups of provided agvs.
    /// </summary>
    /// <param name="agvCorrelationIds">A list of agv correlation ids.</param>
    /// <returns>The most recent ActionGroup for each agv. They may already be completed.</returns>
    Task<IReadOnlyDictionary<Guid, ActionGroup?>> GetCurrentAssignedActionGroupsAsync(IEnumerable<Guid> agvCorrelationIds);

    /// <summary>
    /// Get the ActionGroup.
    /// </summary>
    /// <param name="correlationId">The correlation id of the ActionGroup.</param>
    /// <returns></returns>
    Task<ActionGroup?> GetActionGroupAsync(Guid correlationId);

    /// <summary>
    /// Get the ActionGroup.
    /// </summary>
    /// <param name="correlationId">The correlation id of the ActionGroup.</param>
    /// <exception cref="ActionGroupNotFoundException">If the action group is not found.</exception>
    Task<ActionGroup> GetRequiredActionGroupByIdAsync(Guid correlationId);

    /// <summary>
    /// Get the action group that contains the given action.
    /// </summary>
    /// <param name="correlationId">The action correlation id.</param>
    /// <exception cref="ActionGroupNotFoundException">If the action group is not found.</exception>
    Task<ActionGroup> GetRequiredActionGroupByActionIdAsync(Guid correlationId);

    /// <summary>
    /// Get the AgvAction.
    /// </summary>
    /// <param name="correlationId">The action correlation id.</param>
    /// <returns>Returns AgvAction identified by key or null when not found.</returns>
    [PublicAPI]
    Task<AgvAction?> GetActionByIdAsync(Guid correlationId);

    /// <summary>
    /// Get the AgvAction.
    /// </summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <returns>Returns the AgvAction identified by key.</returns>
    /// <exception cref="ActionNotFoundException">If the action is not found.</exception>
    Task<AgvAction> GetRequiredActionByIdAsync(Guid correlationId);

    /// <summary>
    /// Updates the State and the corresponding Date of the Action and 
    /// the State and the corresponding Date of the ActionGroup the Action is assigned to. 
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="agv">The assigned agv.</param>
    /// <param name="state">The new actionGroupState.</param>
    /// <param name="reason">If set it will be added to the Reasons of the ActionGroup as justification.</param>
    Task<ActionGroup> UpdateActionAndGroupStateAsync(AgvAction action, Models.Agv.Agv? agv, ActionState state, Reason? reason = null);

    /// <summary>
    /// Determine if there is any drop action which is not completed (agv loaded).
    /// </summary>
    /// <param name="agvCorrelationId">The correlation Id of the agv.</param>
    /// <returns>True if there is any not completed drop action for the given agv.</returns>
    Task<bool> HasUnfinishedDropActionsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Determine if there is any subsequent action which has not started.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv .</param>
    /// <returns>True if there is any not started action in the action group.</returns>
    Task<bool> HasUnfinishedActionGroupAsync(Guid agvCorrelationId);

    /// <summary>
    /// Determine if there is any subsequent action which has not started.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv .</param>
    /// <returns>True if there is any not started action in the action group.</returns>
    Task<bool> HasNotStartedActionsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get list of ActionGroup with not yet started actions.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    Task<IReadOnlyList<ActionGroup>> GetNotStartedActionGroupsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get list of ActionGroup with invalid actions.
    /// </summary>
    [PublicAPI]
    Task<IReadOnlyList<ActionGroup>> GetInvalidActionGroupsAsync();

    /// <summary>
    /// Return the subsequent action for the agv that is not started yet.
    /// Generally it is the drop action after the completion of the preceding pick action
    /// or the charge action after the completion of the preceding move action.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    /// <returns>Returns subsequent action</returns>
    Task<AgvAction> GetSubsequentActionAsync(Guid agvCorrelationId);

    /// <summary>
    /// Checks if the agv has any assigned action groups or new ones that are unassigned.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    Task<bool> AgvHasPendingActions(Guid agvCorrelationId);

    /// <summary>
    /// Checks if the agv has any assigned action groups that have priority.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    Task<bool> AgvHasPriorityActionGroups(Guid agvCorrelationId);

    /// <summary>
    /// Gets the agv assigned to the group of the given action.
    /// </summary>
    /// <param name="actionCorrelationId">CorrelationId of the action.</param>
    Task<Guid?> GetAgvAssignedToAction(Guid actionCorrelationId);

    /// <summary>
    /// Checks if the agv has actions that are started.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    Task<bool> HasStartedActionAsync(Guid agvCorrelationId);

    /// <summary>
    /// Update an action group.
    /// </summary>
    /// <param name="actionGroup">The action group to be updated</param>
    /// <returns></returns>
    Task UpdateAsync(ActionGroup actionGroup);

    /// <summary>
    ///  Gets the average deviation between the actual and estimated
    /// duration [s] of an action group for the given target nodes.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="targetNodes">The target nodes.</param>
    /// <returns>The deviation in seconds.</returns>
    Task<double> GetAverageDurationEstimateDeviationAsync(Guid agvCorrelationId, string[] targetNodes );
}