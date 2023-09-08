using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using TransportOrder.Domain.ReadModels;
using ActionGroup = TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace TransportOrder.Domain.Repositories;

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
    Task<List<ActionGroupReadModel>> GetActionGroupsWithTargetNodeTypeAsync(IList<ActionGroupState> states, DateTime? fromDate, DateTime? toDate, int maxItems);

    /// <summary>
    /// Get currently assigned action groups of provided AGVs.
    /// </summary>
    /// <param name="agvCorrelationIds"></param>
    /// <returns></returns>
    public Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> GetCurrentAssignedActionGroupsWithTargetNodeTypeAsync(IEnumerable<Guid> agvCorrelationIds);

    /// <summary>
    /// Enrich an action group with the target node types.
    /// </summary>
    /// <param name="actionGroup">The action group to be enriched.</param>
    public Task<ActionGroupReadModel> EnrichActionGroupWithTargetNodeTypesAsync(ActionGroup actionGroup);

    /// Get an action group with node types by correlation id.
    /// <param name="correlationId">The action group correlation id.</param>
    public Task<ActionGroupReadModel?> GetActionGroupWithTargetNodeTypesAsync(Guid correlationId);
}