using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
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
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> AddAsync(ActionGroup actionGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the action
    /// </summary>
    /// <param name="actionGroup">Agv Action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True, if the action group has been updated. False if it has been added.</returns>
    Task<bool> AddOrUpdateAsync(ActionGroup actionGroup, CancellationToken cancellationToken = default);

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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(IList<ActionGroupState>? states = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 100, Guid? assignedAgvCorrelationId = null, bool? isManual = null, SortBy sortBy = SortBy.CreatedDate, bool sortDescending = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a list of ActionGroup.
    /// </summary>
    /// <param name="actionGroupState">One actionGroupState filter which should be included in the return parameter.</param>
    /// <param name="fromDate">The UTC from date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to date, only items with a less CreatedDate will be considered.</param>
    /// <param name="assignedAgvCorrelationId">The correlation id of assigned agv.</param>
    /// <param name="maxItems">The maximum number of items to return.</param>
    /// <param name="isManual">The action group was created manually or automatic. If null, the filter is not considered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(ActionGroupState actionGroupState, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0, Guid? assignedAgvCorrelationId = null, bool? isManual = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next manual action group for an agv.
    /// </summary>
    /// <param name="assignedAgv">The assigned agv.</param>
    /// <param name="allowedActionTypes">Optional list of action types to be contained in the group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActionGroup?> GetNextManualActionGroupForAgvAsync(Guid assignedAgv, List<ActionType>? allowedActionTypes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the durations of actions in action groups that are in given actionGroupState and
    /// go from a source node to a target node. Both source and target are optional.
    /// </summary>
    /// <param name="actionGroupState">The actionGroupState of the action groups.</param>
    /// <param name="sourceNodeId">The source node id.</param>
    /// <param name="targetNodeId">The target node id.</param>
    /// <param name="maxItems">The maximum number of groups returned.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<Dictionary<ActionType, ICollection<double>>> GetActionDurationsByTypeAsync(ActionGroupState actionGroupState, Guid? sourceNodeId = null, Guid? targetNodeId = null, int maxItems = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get ActionGroup count which have the given actionGroupState (and date range filter).
    /// </summary>
    /// <param name="states">Can be one actionGroupState filter or contain multiple states which should be included in the return</param>
    /// <param name="fromDate">The UTC from date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC to date, only items with a less CreatedDate will be considered.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<int> GetActionGroupsCountAsync(IEnumerable<ActionGroupState> states, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current AgvAction of provided agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current (running) AgvAction of the current ActionGroup.</returns>
    Task<AgvAction?> GetCurrentAgvActionAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get currently assigned action groups of provided agv.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The most recent ActionGroup of the agv. It may already be completed.</returns>
    [PublicAPI]
    Task<ActionGroup?> GetCurrentAssignedActionGroupAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if the agv has currently assigned action groups that are created or started.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [PublicAPI]
    Task<bool> HasCurrentAssignedActionGroupAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get currently assigned action groups of provided agvs.
    /// </summary>
    /// <param name="agvCorrelationIds">A list of agv correlation ids.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The most recent ActionGroup for each agv. They may already be completed.</returns>
    Task<IReadOnlyDictionary<Guid, ActionGroup?>> GetCurrentAssignedActionGroupsAsync(IEnumerable<Guid> agvCorrelationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the ActionGroup.
    /// </summary>
    /// <param name="correlationId">The correlation id of the ActionGroup.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Returns ActionGroup with the given correlation id or null when not found.</returns>
    Task<ActionGroup?> GetActionGroupAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the handling units that match the given query.
    /// </summary>
    /// <param name="query">The substring to search for (case-insensitive).</param>
    /// <param name="maxItems">The maximum number of handling unit results to return.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<string>> GetHandlingUnitsAsync(string query, int maxItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the ActionGroup.
    /// </summary>
    /// <param name="correlationId">The correlation id of the ActionGroup.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ActionGroupNotFoundException">If the action group is not found.</exception>
    Task<ActionGroup> GetRequiredActionGroupByIdAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the action group that contains the given action.
    /// </summary>
    /// <param name="correlationId">The action correlation id.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ActionGroupNotFoundException">If the action group is not found.</exception>
    Task<ActionGroup> GetRequiredActionGroupByActionIdAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all parking or charging action groups.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Action groups that are going to parkers or chargers.</returns>
    Task<ICollection<ActionGroup>> GetRunningIdleActionGroups(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all charging action groups that are actually charging.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Action groups that charging at chargers.</returns>
    Task<ICollection<ActionGroup>> GetRunningChargeActionGroups(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the AgvAction.
    /// </summary>
    /// <param name="correlationId">The action correlation id.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Returns AgvAction identified by key or null when not found.</returns>
    [PublicAPI]
    Task<AgvAction?> GetActionByIdAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the AgvAction.
    /// </summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Returns the AgvAction identified by key.</returns>
    /// <exception cref="ActionNotFoundException">If the action is not found.</exception>
    Task<AgvAction> GetRequiredActionByIdAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the State and the corresponding Date of the Action and 
    /// the State and the corresponding Date of the ActionGroup the Action is assigned to. 
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="agv">The assigned agv.</param>
    /// <param name="state">The new actionGroupState.</param>
    /// <param name="reason">If set it will be added to the Reasons of the ActionGroup as justification.</param>
    /// <param name="now"></param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<ActionGroup> UpdateActionAndGroupStateAsync(AgvAction action, Agv? agv, ActionState state, Reason? reason, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine if there is any drop action which is not completed (agv loaded).
    /// </summary>
    /// <param name="agvCorrelationId">The correlation Id of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if there is any not completed drop action for the given agv.</returns>
    Task<bool> HasUnfinishedDropActionsAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine if there is any subsequent action which has not started.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv .</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if there is any not started action in the action group.</returns>
    Task<bool> HasUnfinishedActionGroupAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of ActionGroup with not yet started actions.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv .</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<List<ActionGroup>> GetNotStartedActionsAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine if there is any subsequent action which has not started.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv .</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if there is any not started action in the action group.</returns>
    Task<bool> HasNotStartedActionsAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of ActionGroup with not yet started actions.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IReadOnlyList<ActionGroup>> GetNotStartedActionGroupsAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of ActionGroup with invalid actions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [PublicAPI]
    Task<IReadOnlyList<ActionGroup>> GetInvalidActionGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Return the subsequent action for the agv that is not started yet.
    /// Generally it is the drop action after the completion of the preceding pick action
    /// or the charge action after the completion of the preceding move action.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns subsequent action</returns>
    Task<AgvAction> GetSubsequentActionAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the agv has any assigned action groups or new ones that are unassigned.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<bool> AgvHasPendingActions(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the agv assigned to the group of the given action.
    /// </summary>
    /// <param name="actionCorrelationId">CorrelationId of the action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<Guid?> GetAgvAssignedToAction(Guid actionCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the agv has actions that are started.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<bool> HasStartedActionAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an action group.
    /// </summary>
    /// <param name="actionGroup">The action group to be updated</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task UpdateAsync(ActionGroup actionGroup, CancellationToken cancellationToken = default);

    /// <summary>
    ///  Gets the average deviation between the actual and estimated
    /// duration [s] of an action group for the given target nodes.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="targetNodes">The target nodes.</param>
    /// <returns>The deviation in seconds.</returns>
    Task<double> GetAverageDurationEstimateDeviationAsync(Guid agvCorrelationId, Guid[] targetNodes);

    /// <summary>
    /// True, if the last completed action of this agv was a move to a coordinate.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<bool> IsLastCompletedActionMoveToCoordinate(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the DockFront value of the last completed action of an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="currentActionGroupCorrelationId">The correlation id of the current action group.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The DockFront value of the action.</returns>
    Task<bool?> GetDockFrontOfLastCompletedAction(Guid agvCorrelationId, Guid currentActionGroupCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last aborted pick or drop action of an AGV.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The last aborted pick or drop action.</returns>
    Task<AgvAction?> GetLastAbortedPickDropActionAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns boolean specifying that an AGV already has a charging group in created state.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<bool> HasCreatedChargingActionGroupForAgvAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the started action group for an AGV assuming one exists.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Started action group.</returns>
    Task<ActionGroup?> GetStartedActionGroupForAgvAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of action groups that are assigned to an AGV in created or started state.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Action groups.</returns>
    Task<IReadOnlyList<ActionGroup>> GetCreatedAndStartedActionGroupsForAgvAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get started or paused pick/drop and assigned parking/charging action groups for station.
    /// </summary>
    /// <param name="stationName">The name of the station.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IReadOnlyCollection<ActionGroup>> GetCurrentActionGroupsForStation(string stationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paused pick/drop action groups for station.
    /// </summary>
    /// <param name="stationName">The name of the station.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IReadOnlyCollection<ActionGroup>> GetPausedActionGroupsForStation(string stationName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of action groups that are assigned to an AGV in created, paused, or started state.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Action groups.</returns>
    Task<ICollection<ActionGroup>> GetPendingActionGroupsForAgvAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns agvs that have priority action groups.
    /// </summary>
    /// <param name="agvs">The agv correlation ids.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Collection of agv correlation id that have priority action groups. </returns>
    Task<ICollection<Guid>> FilterAgvsWithPriorityActionGroups(ICollection<Guid> agvs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of running or pending action groups that are heading towards the given target.
    /// </summary>
    /// <param name="targetId">The target node.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Action groups.</returns>
    Task<ICollection<ActionGroup>> GetCurrentActionGroupsToTarget(Guid targetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all the not started action groups for a specific agv that are targeting one of the nodes of a collection.
    /// </summary>
    /// <param name="agvId">The agv to check.</param>
    /// <param name="targets">The targets to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<ICollection<ActionGroup>> GetNotStartedActionGroupsToTargetsForAgv(Guid agvId, ICollection<Guid> targets, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next automatic action group picking at given node.
    /// </summary>
    /// <param name="nodeId">The id of the node.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Action group, if available</returns>
    Task<ActionGroup?> GetNextPickAtNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unstarted orders that pick at the given target node and are assigned to one of the given agvs.
    /// </summary>
    /// <param name="targetId">The id of the target node.</param>
    /// <param name="assignedAgvs">A collection of agv correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of action groups.</returns>
    Task<ICollection<ActionGroup>> GetNextPicksAtTarget(Guid targetId, ICollection<Guid> assignedAgvs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next dropping action group that can run a Doppelspiel at one of the given station names.
    /// </summary>
    /// <param name="stations">The Doppelspiel stations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An action group.</returns>
    Task<ActionGroup?> GetNextDropActionGroupEligibleForDoppelspiel(ICollection<string> stations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next delayed drop for the given stations.
    /// </summary>
    /// <param name="stations">The station ids.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<ActionGroup?> GetNextDelayedDropForStations(ICollection<Guid> stations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next delayed action group picking the given stations.
    /// </summary>
    /// <param name="pickNode">The station id.</param>
    /// <param name="ordersToIgnore">The orders that should not be considered.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<ActionGroup?> GetNextPickForStation(GraphNode pickNode, ICollection<Guid>? ordersToIgnore = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next drop order that was registered for Doppelspiel, but has no matching partner.
    /// </summary>
    /// <param name="node">The Doppelspiel station node.</param>
    /// <param name="orderToIgnore">The drop order to ignore.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<ActionGroup?> GetNextUnmatchedDoppelspielDrop(GraphNode node, Guid? orderToIgnore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next pick order that was registered for Doppelspiel, but has no matching partner.
    /// </summary>
    /// <param name="node">The Doppelspiel station node.</param>
    /// <param name="orderToIgnore">The drop order to ignore.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<ActionGroup?> GetNextUnmatchedDoppelspielPick(GraphNode node, Guid? orderToIgnore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if there are any queued or currently running host orders for a station group given by sources and targets.
    /// </summary>
    /// <param name="stations">The stations of the station group.</param>
    /// <param name="agvCorrelationId">The correlation id of the AGV that is excluded from the check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True, if there are host orders (of a different or unassigned Agv) to this station group already queued or running. This includes not running, but queued orders.</returns>
    Task<bool> HasActionGroupsForStationGroup(ICollection<Guid> stations, Guid? agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True, if the given action group is queued.
    /// </summary>
    /// <param name="correlationId">The correlation id of the action group</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<bool> IsActionGroupQueued(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest action groups for an AGV in Created, Started, and Completed states.
    /// Returns at most one action group per state.
    /// </summary>
    /// <param name="agvCorrelationId">The AGV correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A record containing the latest action group for each state (if available).</returns>
    Task<AgvActionGroupsByState> GetLatestActionGroupsByStateForAgvAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filter assigned agvs that have a created or started pick action group.
    /// </summary>
    /// <param name="assignedAgvs">List of agv ids which have assigned running idle action groups.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Filtered list of agv ids which have additionally created or started pick action.</returns>
    Task<ICollection<Guid>> GetAgvsMovingCargo(ICollection<Guid> assignedAgvs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete action groups older than given time.
    /// </summary>
    /// <param name="outDatedTime">The time an action group gets outdated.</param>
    Task DeleteOutdatedActionGroupsAsync(DateTime outDatedTime);
}