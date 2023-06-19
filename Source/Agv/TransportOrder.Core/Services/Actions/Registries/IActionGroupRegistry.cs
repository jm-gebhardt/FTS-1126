using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

// ReSharper disable UnusedMemberInSuper.Global
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries
{
    /// <summary>
    /// Interface for ActionGroup and AgvAction registry
    /// </summary>
    public interface IActionGroupRegistry
    {
        /// <summary>
        /// Saves the action
        /// </summary>
        /// <param name="actionGroup">Agv Action</param>
        Task InsertAsync(ActionGroup actionGroup);

        /// <summary>
        /// Get ActionGroup
        /// </summary>
        /// <param name="states">Can be one state filter or contain multiple states which should be included in the return. If null, the filter is inactive</param>
        /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered</param>
        /// <param name="toDate">The UTC from Date, only items with a less CreatedDate will be considered</param>
        /// <param name="maxItems">The max item count to return</param>
        /// <returns></returns>
        public Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(IList<ActionGroupState>? states = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0);

        /// <summary>
        /// Get ActionGroup count which have the given state (and date range filter)
        /// </summary>
        /// <param name="states">Can be one state filter or contain multiple states which should be included in the return</param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        public Task<int> GetActionGroupsCountAsync(IEnumerable<ActionGroupState> states, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get currently assigned action groups of provided AGVs
        /// </summary>
        /// <param name="agvCorrelationIds"></param>
        /// <returns></returns>
        public Task<IReadOnlyDictionary<Guid, ActionGroup?>> GetCurrentAssignedActionGroupsAsync(IEnumerable<Guid> agvCorrelationIds);

        /// <summary>
        /// Get ActionGroup
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public Task<ActionGroup?> GetActionGroupAsync(Guid correlationId);

        /// <summary>
        /// Get ActionGroup
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public Task<ActionGroup> GetRequiredActionGroupByIdAsync(Guid correlationId);

        /// <summary>
        /// Get Agv action by key.
        /// </summary>
        /// <param name="correlationId">Unique ID per action</param>
        /// <returns>Returns Agv action identified by key or null when not found</returns>
        public Task<AgvAction?> GetActionByIdAsync(Guid correlationId);

        /// <summary>
        /// Get Agv action by key.
        /// </summary>
        /// <param name="correlationId">Unique ID per action</param>
        /// <returns>Returns Agv action identified by key or throws when not found</returns>
        public Task<AgvAction> GetRequiredActionByIdAsync(Guid correlationId);

        /// <summary>
        /// Delete all actions with the same parent id
        /// </summary>
        /// <param name="parentId">Correlation id of the parent object</param>
        public Task DeleteByParentIdAsync(Guid parentId);
        
        /// <summary>
        /// Updates the Action State/Date of the existing ActionGroup and also the ActionGroup State/Date
        /// </summary>
        /// <param name="action">The action to be started</param>
        /// <param name="agv">The assigned Agv</param>
        /// <param name="state">The new state</param>
        public Task<ActionGroupState> UpdateActionAndGroupStateAsync(AgvAction action, IAgv agv, ActionState state);

        /// <summary>
        /// Determine if there is any drop action which is not completed (Agv loaded)
        /// </summary>
        /// <param name="agvCorrelationId">The AgvCorrelationId of the agv to check</param>
        /// <returns>True if there is any not completed drop action for the given Agv</returns>
        Task<bool> HasUnfinishedDropActionsAsync(Guid agvCorrelationId);

        /// <summary>
        /// Get ActionGroups where no action was started yet
        /// </summary>
        public Task<IReadOnlyList<ActionGroup>> GetNotStartedActionGroupsAsync();

        /// <summary>
        /// Return the subsequent action for the Agv that is not started yet.
        /// So that's normally the drop action after the completion of the preceding pick action
        /// </summary>
        /// <param name="agvCorrelationId"></param>
        /// <returns>Returns subsequent action</returns>
        public Task<AgvAction> GetSubsequentActionAsync(Guid agvCorrelationId);
    }
}