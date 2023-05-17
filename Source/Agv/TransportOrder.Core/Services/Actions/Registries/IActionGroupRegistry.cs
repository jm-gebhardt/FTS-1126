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
        /// <param name="states">Can be one state filter or contain multiple states which should be included in the return</param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="maxItems"></param>
        /// <returns></returns>
        public Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(IEnumerable<ActionGroupState> states, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0);

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
        /// Updates the action of the existing ActionGroup and also the ActionGroup State
        /// </summary>
        /// <param name="action">The action to be started</param>
        /// <param name="agv">The assigned Agv</param>
        /// <param name="state">The new state</param>
        public Task UpdateActionAndGroupStateAsync(AgvAction action, IAgv agv, ActionState state);

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