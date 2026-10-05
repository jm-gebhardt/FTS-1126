using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;
using Microsoft.EntityFrameworkCore;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class ActionGroupRetrievalService : IActionGroupRetrievalService
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <inheritdoc cref="FilterableBinder{TDomainObject,TReadModel}.FilterByPaginatedWithOrdering"/>
    private readonly FilterableBinder<ActionGroup, ActionGroupFilterable>.FilterByPaginatedWithOrdering _filter;

    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupRetrievalService"/> instance.
    /// </summary>
    public ActionGroupRetrievalService(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _filter = FilterableBinder<ActionGroup, ActionGroupFilterable>.CreateFilterWithPagination(_dbContextFactory);
        _mapper = mapper;
    }

    private async Task<TransportOrderDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        // no need for tracking as we are returning read models only
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return context;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FilteredActionGroup>> Filter(IFilter filter, int pageNr, int pageSize, SortDirection order, ICollection<string> orderProperties, CancellationToken cancellationToken = default)
    {
        return _mapper.Map<PagedResult<ActionGroupFilterable>, PagedResult<FilteredActionGroup>>(await _filter.Invoke(filter, pageNr, pageSize, order, orderProperties, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ActionGroupReadModel>> GetActionGroupsWithNodesAsync(IReadOnlyCollection<ActionGroupState> states, DateTime? fromDate, DateTime? toDate, int maxItems, ICollection<Guid>? assignedAgvIds = null, SortBy sortBy = SortBy.CreatedDate, bool sortDescending = true, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateDbContextAsync(cancellationToken);
        var query = context.Set<ActionGroup>().AsQueryable();

        if (states.Count != 0)
        {
            query = query.Where(g => states.Contains(g.State));
        }

        if (fromDate != null && fromDate != DateTime.MinValue)
        {
            query = query.Where(g => g.CreatedDate >= fromDate);
        }

        if (toDate != null && toDate != DateTime.MaxValue)
        {
            query = query.Where(g => g.CreatedDate <= toDate);
        }

        if (assignedAgvIds != null)
        {
            query = query.Where(g => g.AssignedAgv != null && assignedAgvIds.Contains((Guid)g.AssignedAgv));
        }

        query = query.OrderByProperty(sortBy.ToString(), isDescending: sortDescending).ThenByDescending(g => g.IsManual);

        var actionGroups = await query
            .Take(maxItems)
            .ToListAsync(cancellationToken: cancellationToken);

        var targetNodeIds = actionGroups.SelectMany(g => g.Actions).Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!.Value).Distinct().ToList();

        var graphNodesById = await context.Set<GraphNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n, cancellationToken);

        var result = actionGroups.Select(g => new ActionGroupReadModel(
            g.CorrelationId,
            g.OrderId,
            g.AssignedAgv,
            g.CreatedDate,
            g.StartedDate,
            g.CompletedDate,
            g.EstimatedDuration,
            g.State,
            g.Creator,
            g.Priority,
            g.HandlingUnit,
            g.Actions.Select(a =>
            {
                if (a.Target.NodeId != null)
                {
                    bool hasNode = graphNodesById.TryGetValue(a.Target.NodeId!.Value, out var node);
                    return new AgvActionReadModel(
                        a.CorrelationId,
                        a.ParentId,
                        a.Sequence,
                        a.State,
                        a.ActionType,
                        a.Target.NodeId!.Value,
                        hasNode ? node!.Name : "",
                        graphNodesById.Count == 0 || !hasNode ?
                            AgvGraph.NodeType.WayPoint : // WayPoint is the default value for not available targetNodeIds
                            node!.Type.GetTypeForActionType(a.ActionType),
                        a.Nodes.Select(n => n.Name).ToArray(),
                        a.Edges,
                        a.CreatedDate,
                        a.StartedDate,
                        a.CompletedDate,
                        a.PausedDate,
                        a.PriorRunAndPauseDuration,
                        a.ShouldSkip,
                        a.InfoToAgv,
                        a.AdditionalInfo);
                }
                return new AgvActionReadModel(
                    a.CorrelationId,
                    a.ParentId,
                    a.Sequence,
                    a.State,
                    a.ActionType,
                    Guid.Empty,
                    string.Empty,
                    AgvGraph.NodeType.WayPoint, // WayPoint is the default value for not available targetNodeIds
                    a.Nodes.Select(n => n.Name).ToArray(),
                    a.Edges,
                    a.CreatedDate,
                    a.StartedDate,
                    a.CompletedDate,
                    a.PausedDate,
                    a.PriorRunAndPauseDuration,
                    a.ShouldSkip,
                    a.InfoToAgv,
                    a.AdditionalInfo);
            }).Distinct().ToList(),
            g.IsManual,
            g.IsQueued,
            g.ShouldEscapeQueue,
            g.Reasons,
            g.DoNotPause
        )).ToList();

        return result;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> GetCurrentActionGroupsWithNodesAsync(IReadOnlyCollection<Guid> agvCorrelationIds)
    {
        await using var context = await CreateDbContextAsync();
        var actionGroups = await context.Set<ActionGroup>()
            .Where(g => g.AssignedAgv != null && agvCorrelationIds.Contains((Guid)g.AssignedAgv) && (g.State == ActionGroupState.Started || g.State == ActionGroupState.Paused))
            .OrderByDescending(g => g.StartedDate)
            .ToListAsync();

        var targetNodeIds = actionGroups.SelectMany(g => g.Actions.Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!.Value)).Distinct().ToList();

        var graphNodes = await context.Set<GraphNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        var result = agvCorrelationIds.ToDictionary(
            agvCorrelationId => agvCorrelationId,
            agvCorrelationId =>
            {
                var actionGroup = actionGroups.FirstOrDefault(g => g.AssignedAgv == agvCorrelationId);
                if (actionGroup == null)
                {
                    return null;
                }

                return new ActionGroupReadModel(actionGroup.CorrelationId,
                    actionGroup.OrderId,
                    actionGroup.AssignedAgv,
                    actionGroup.CreatedDate,
                    actionGroup.StartedDate,
                    actionGroup.CompletedDate,
                    actionGroup.EstimatedDuration,
                    actionGroup.State,
                    actionGroup.Creator,
                    actionGroup.Priority,
                    actionGroup.HandlingUnit,
                    GetAgvActionReadModel(actionGroup, graphNodes),
                    actionGroup.IsManual,
                    actionGroup.IsQueued,
                    actionGroup.ShouldEscapeQueue,
                    actionGroup.Reasons,
                    actionGroup.DoNotPause
                );
            });

        return result;
    }

    private static List<AgvActionReadModel> GetAgvActionReadModel(ActionGroup actionGroup, IReadOnlyCollection<GraphNode> graphNodes)
    {
        return actionGroup.Actions.GroupJoin(graphNodes, a => a.Target.NodeId, g => g.Id, (action, nodes) => new { action, node = nodes.SingleOrDefault() }).Select(an => new AgvActionReadModel(
            an.action.CorrelationId,
            an.action.ParentId,
            an.action.Sequence,
            an.action.State,
            an.action.ActionType,
            an.node?.Id ?? Guid.Empty,
            an.node?.Name ?? string.Empty,
            an.node?.Type.GetTypeForActionType(an.action.ActionType) ?? AgvGraph.NodeType.WayPoint,
            an.action.Nodes.Select(n => n.Name).ToArray(),
            an.action.Edges,
            an.action.CreatedDate,
            an.action.StartedDate,
            an.action.CompletedDate,
            an.action.PausedDate,
            an.action.PriorRunAndPauseDuration,
            an.action.ShouldSkip,
            an.action.InfoToAgv,
            an.action.AdditionalInfo
        )).ToList();
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel> EnrichActionGroupWithNodesAsync(ActionGroup actionGroup)
    {
        await using var context = await CreateDbContextAsync();

        var targetNodeIds = actionGroup.Actions.Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!.Value).Distinct().ToList();

        var graphNodes = await context.Set<GraphNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        var result = new ActionGroupReadModel(actionGroup.CorrelationId,
            actionGroup.OrderId,
            actionGroup.AssignedAgv,
            actionGroup.CreatedDate,
            actionGroup.StartedDate,
            actionGroup.CompletedDate,
            actionGroup.EstimatedDuration,
            actionGroup.State,
            actionGroup.Creator,
            actionGroup.Priority,
            actionGroup.HandlingUnit,
            GetAgvActionReadModel(actionGroup, graphNodes),
            actionGroup.IsManual,
            actionGroup.IsQueued,
            actionGroup.ShouldEscapeQueue,
            actionGroup.Reasons,
            actionGroup.DoNotPause
        );

        return result;
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel?> GetActionGroupWithNodesAsync(Guid correlationId)
    {
        await using var context = await CreateDbContextAsync();

        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
        var actionGroup = actionGroups.FirstOrDefault(g => g.CorrelationId == correlationId);
        if (actionGroup == null)
        {
            return null;
        }

        var targetNodeIds = actionGroup.Actions.Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!.Value).Distinct().ToList();

        var graphNodes = await context.Set<GraphNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        return new ActionGroupReadModel(actionGroup.CorrelationId,
            actionGroup.OrderId,
            actionGroup.AssignedAgv,
            actionGroup.CreatedDate,
            actionGroup.StartedDate,
            actionGroup.CompletedDate,
            actionGroup.EstimatedDuration,
            actionGroup.State,
            actionGroup.Creator,
            actionGroup.Priority,
            actionGroup.HandlingUnit,
            GetAgvActionReadModel(actionGroup, graphNodes),
            actionGroup.IsManual,
            actionGroup.IsQueued,
            actionGroup.ShouldEscapeQueue,
            actionGroup.Reasons,
            actionGroup.DoNotPause
        );
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel?> GetCurrentAgvActionGroupWithNodesAsync(Guid agvCorrelationId)
    {
        await using var context = await CreateDbContextAsync();

        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
        var actionGroup = actionGroups.Where(g => g.AssignedAgv == agvCorrelationId && (g.State == ActionGroupState.Started || g.State == ActionGroupState.Paused)).OrderByDescending(ag => ag.StartedDate).FirstOrDefault();
        if (actionGroup == null)
        {
            return null;
        }

        var targetNodeIds = actionGroups.Where(g => g.AssignedAgv == agvCorrelationId).SelectMany(g => g.Actions).Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!.Value).Distinct().ToList();

        var graphNodes = await context.Set<GraphNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        return new ActionGroupReadModel(actionGroup.CorrelationId,
            actionGroup.OrderId,
            actionGroup.AssignedAgv,
            actionGroup.CreatedDate,
            actionGroup.StartedDate,
            actionGroup.CompletedDate,
            actionGroup.EstimatedDuration,
            actionGroup.State,
            actionGroup.Creator,
            actionGroup.Priority,
            actionGroup.HandlingUnit,
            GetAgvActionReadModel(actionGroup, graphNodes),
            actionGroup.IsManual,
            actionGroup.IsQueued,
            actionGroup.ShouldEscapeQueue,
            actionGroup.Reasons,
            actionGroup.DoNotPause
        );
    }

    /// <inheritdoc />
    public async Task<bool> IsAgvHeadingToNodeOfType(Guid agvCorrelationId, AgvGraph.NodeType nodeType)
    {
        await using var context = await CreateDbContextAsync();

        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
        IQueryable<GraphNode> graphNodes = context.Set<GraphNode>();

        var actionGroupsResult = await actionGroups
            .Where(g => g.AssignedAgv == agvCorrelationId && g.State == ActionGroupState.Started)
            .Include(g => g.Actions)
            .ThenInclude(a => a.Target)
            .ToListAsync();

        return actionGroupsResult.Any(g => g.Actions.Join(
            graphNodes.AsEnumerable(),
            a => a.Target.Node?.Id,
            n => n.Id,
            (_, n) => n
        ).Any(n => n.Type.IsOfType(nodeType)));
    }
}