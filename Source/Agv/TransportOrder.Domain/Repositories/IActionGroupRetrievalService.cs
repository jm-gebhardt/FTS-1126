using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

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
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyCollection<ActionGroupReadModel>> GetActionGroupsWithNodesAsync(IReadOnlyCollection<ActionGroupState> states, DateTime? fromDate, DateTime? toDate, int maxItems, ICollection<Guid>? assignedAgvIds = null, SortBy sortBy = SortBy.CreatedDate, bool sortDescending = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current action groups of provided AGVs.
    /// </summary>
    /// <param name="agvCorrelationIds">The AGV correlation ids.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> GetCurrentActionGroupsWithNodesAsync(IReadOnlyCollection<Guid> agvCorrelationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrich an action group with the target node types.
    /// </summary>
    /// <param name="actionGroup">The action group to be enriched.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<ActionGroupReadModel> EnrichActionGroupWithNodesAsync(ActionGroup actionGroup, CancellationToken cancellationToken = default);

    /// Get an action group with node types by correlation id.
    /// <param name="correlationId">The action group correlation id.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<ActionGroupReadModel?> GetActionGroupWithNodesAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// Get the current action group for the agv with node types by correlation id.
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<ActionGroupReadModel?> GetCurrentAgvActionGroupWithNodesAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a flag if AGV is heading to nodeType (e.g. Park or Charge).
    /// </summary>
    /// <param name="agvCorrelationId">The AGV correlation id.</param>
    /// <param name="nodeType">The node type to check.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public Task<bool> IsAgvHeadingToNodeOfType(Guid agvCorrelationId, AgvGraph.NodeType nodeType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a filtered paginated collection of <see cref="FilteredActionGroup"/>
    /// </summary>
    /// <param name="filter">The <see cref="IFilter"/> to use.</param>
    /// <param name="pageNr">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="order">The direction to order.</param>
    /// <param name="orderProperties">The properties to order by.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<PagedResult<FilteredActionGroup>> Filter(IFilter filter, int pageNr, int pageSize, SortDirection order, ICollection<string> orderProperties, CancellationToken cancellationToken = default);
}