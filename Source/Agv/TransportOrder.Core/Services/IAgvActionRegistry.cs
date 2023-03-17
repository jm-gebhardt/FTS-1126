using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

// ReSharper disable UnusedMemberInSuper.Global
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Abstracted interface for agv action registry
    /// </summary>
    public interface IAgvActionRegistry
    {
        /// <summary>
        /// Insert action into database
        /// </summary>
        /// <param name="agvAction">Agv Action</param>
        public void Insert(AgvAction agvAction);

        /// <summary>
        /// Get agv action by key.
        /// </summary>
        /// <param name="id">Unique ID per action</param>
        /// <returns>Returns agv action identified by key or null when not found</returns>
        public AgvAction? GetActionById(Guid id);

        /// <summary>
        /// Return list of all actions not started yet
        /// </summary>
        /// <param name="sortBySequenceAsc">If true, then pending actions get sorted according to their sequence number</param>
        /// <returns>Returns the list of pending actions, empty list when there are no pending actions</returns>
        public List<AgvAction> GetPendingActions(bool sortBySequenceAsc);

        /// <summary>
        /// Return list of all action belonging to the same parent id
        /// </summary>
        /// <param name="parentId">Correlation ID of the parent object</param>
        /// <returns>Returns the list of actions, empty list when there are no actions</returns>
        public List<AgvAction> GetActionsByParentId(Guid parentId);

        /// <summary>
        /// Updates state of an AGV action
        /// </summary>
        /// <param name="id">Id of agv action</param>
        /// <param name="state">New state</param>
        /// <returns>True if state was update, false if</returns>
        public bool UpdateState(Guid id, ActionState state);

        /// <summary>
        /// Return the subsequent action for a parent object that is not started yet
        /// </summary>
        /// <param name="parentId">Correlation ID of the parent object</param>
        /// <returns>Returns subsequent action or null when there are no more subsequent actions</returns>
        public AgvAction? GetSubsequentAction(Guid parentId);

        /// <summary>
        /// Delete all actions with the same parent id
        /// </summary>
        /// <param name="parentId">Correlation id of the parent object</param>
        public void DeleteByParentId(Guid parentId);
    }
}
