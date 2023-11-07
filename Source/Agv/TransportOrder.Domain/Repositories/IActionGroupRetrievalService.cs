using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a read-model repository for Action Group retrieval across tables.
/// </summary>
public interface IActionGroupRetrievalService
{
    /// <summary>
    /// Gets the action groups passing the filter enriched with the target node types.
    /// </summary>
    /// <param name="states">The states to filter for.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater CreatedDate will be considered.</param>
    /// <param name="toDate">The UTC from Date, only items with a less CreatedDate will be considered.</param>
    /// <param name="maxItems">The maximum number of action groups returned.</param>
    /// <param name="assignedAgvIds">If set, only action groups which are assigned to one of these agvs will be returned.</param>
    /// <param name="sortBy">The sorting parameter.</param>
    /// <param name="sortDescending">If true, sorts in descending order.</param>
    Task<IReadOnlyCollection<ActionGroupReadModel>> GetActionGroupsWithNodesAsync(IReadOnlyCollection<ActionGroupState> states, DateTime? fromDate, DateTime? toDate, int maxItems, ICollection<Guid>? assignedAgvIds = null, SortBy sortBy = SortBy.CreatedDate, bool sortDescending = true);

    /// <summary>
    /// Get currently assigned action groups of provided AGVs.
    /// </summary>
    /// <param name="agvCorrelationIds"></param>
    /// <returns></returns>
    public Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> GetAssignedActionGroupsWithNodesAsync(IReadOnlyCollection<Guid> agvCorrelationIds);

    /// <summary>
    /// Enrich an action group with the target node types.
    /// </summary>
    /// <param name="actionGroup">The action group to be enriched.</param>
    public Task<ActionGroupReadModel> EnrichActionGroupWithNodesAsync(ActionGroup actionGroup);

    /// Get an action group with node types by correlation id.
    /// <param name="correlationId">The action group correlation id.</param>
    public Task<ActionGroupReadModel?> GetActionGroupWithNodesAsync(Guid correlationId);

    /// <summary>
    /// Gets a flag if AGV is heading to nodeType (e.g. Park or Charge).
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="nodeType"></param>
    /// <returns></returns>
    public Task<bool> IsAgvHeadingToNodeOfType(Guid agvCorrelationId, AgvGraph.NodeType nodeType);
}