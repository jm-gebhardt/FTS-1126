using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class ActionGroupRetrievalService : IActionGroupRetrievalService
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupRetrievalService"/> instance.
    /// </summary>
    public ActionGroupRetrievalService(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<TransportOrderDbContext> CreateDbContextAsync()
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        // no need for tracking as we are returning read models only
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ActionGroupReadModel>> GetActionGroupsWithNodesAsync(IReadOnlyCollection<ActionGroupState> states, DateTime? fromDate, DateTime? toDate, int maxItems, ICollection<Guid>? assignedAgvIds = null, SortBy sortBy = SortBy.CreatedDate, bool sortDescending = true)
    {
        await using var context = await CreateDbContextAsync();
        IQueryable<ActionGroup> query = context.Set<ActionGroup>().AsQueryable();

        if (states.Any())
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
            query = query.Where(g => g.AssignedAgv != null && assignedAgvIds.Contains((Guid) g.AssignedAgv));
        }

        query = query.OrderByProperty(sortBy.ToString(), isDescending: sortDescending).ThenByDescending(g => g.IsManual);

        List<ActionGroup> actionGroups = await query
            .Take(maxItems)
            .ToListAsync();

        List<string> targetNodeIds = actionGroups.SelectMany(g => g.Actions).Select(a => a.TargetNodeId).Distinct().ToList();

        Dictionary<string, EfNode> graphNodesById = await context.Set<EfNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .GroupBy(n => n.Id)
            // here we expect an EfNode for each Agv, as every Agv has its own graph.
            // Later once we have a merged graph for the whole system, we want to take the Node out of it
            .Select(group => group.First())
            .ToDictionaryAsync(n => n.Id, n => n);

        List<ActionGroupReadModel> result = actionGroups.Select(g => new ActionGroupReadModel(
            g.CorrelationId,
            g.OrderId,
            g.AssignedAgv,
            g.CreatedDate,
            g.StartedDate,
            g.CompletedDate,
            g.State,
            g.Creator,
            g.Actions.Select(a =>
                new AgvActionReadModel(
                    a.CorrelationId,
                    a.ParentId,
                    a.Sequence,
                    a.State,
                    a.ActionType,
                    a.TargetNodeId,
                    graphNodesById[a.TargetNodeId].Type,
                    a.Nodes,
                    a.CreatedDate,
                    a.StartedDate,
                    a.CompletedDate)
            ).Distinct().ToList(),
            g.IsManual
        )).ToList();

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> GetAssignedActionGroupsWithNodesAsync(IReadOnlyCollection<Guid> agvCorrelationIds)
    {
        // Once Agv is assigned to ActionGroup, Status is immediately set to started (and StartedDate), therefore we can use StartedDate to get current assigned ActionGroup
        await using var context = await CreateDbContextAsync();
        List<ActionGroup> actionGroups = await context.Set<ActionGroup>()
            .Where(g => g.AssignedAgv != null && agvCorrelationIds.Contains((Guid) g.AssignedAgv))
            .OrderByDescending(g => g.StartedDate)
            .ToListAsync();

        List<string> targetNodeIds = actionGroups.SelectMany(g => g.Actions.Select(a => a.TargetNodeId)).Distinct().ToList();

        List<EfNode> graphNodes = await context.Set<EfNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        Dictionary<Guid, ActionGroupReadModel?> result = agvCorrelationIds.ToDictionary(
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
                    actionGroup.State,
                    actionGroup.Creator,
                    actionGroup.Actions.Join(graphNodes, a => a.TargetNodeId, n => n.Id, (a, n) =>
                            new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                                a.TargetNodeId, n.Type, a.Nodes, a.CreatedDate, a.StartedDate, a.CompletedDate))
                        .Distinct()
                        .ToList(),
                    actionGroup.IsManual);
            });

        return result;
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel> EnrichActionGroupWithNodesAsync(ActionGroup actionGroup)
    {
        await using var context = await CreateDbContextAsync();

        List<string> targetNodeIds = actionGroup.Actions.Select(a => a.TargetNodeId).Distinct().ToList();

        List<EfNode> graphNodes = await context.Set<EfNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        var result = new ActionGroupReadModel(actionGroup.CorrelationId,
            actionGroup.OrderId,
            actionGroup.AssignedAgv,
            actionGroup.CreatedDate,
            actionGroup.StartedDate,
            actionGroup.CompletedDate,
            actionGroup.State,
            actionGroup.Creator,
            actionGroup.Actions.Join(graphNodes, a => a.TargetNodeId, n => n.Id, (a, n) =>
                    new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                        a.TargetNodeId, n.Type, a.Nodes, a.CreatedDate, a.StartedDate, a.CompletedDate))
                .Distinct()
                .ToList(),
            actionGroup.IsManual);

        return result;
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel?> GetActionGroupWithNodesAsync(Guid correlationId)
    {
        await using var context = await CreateDbContextAsync();

        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
        IQueryable<EfNode> graphNodes = context.Set<EfNode>();
        var result = await actionGroups
            .Where(g => g.CorrelationId == correlationId)
            .Select(g => new ActionGroupReadModel(g.CorrelationId,
                g.OrderId,
                g.AssignedAgv,
                g.CreatedDate,
                g.StartedDate,
                g.CompletedDate,
                g.State,
                g.Creator,
                g.Actions.Join(graphNodes, a => a.TargetNodeId, n => n.Id, (a, n) =>
                        new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                            a.TargetNodeId, n.Type, a.Nodes, a.CreatedDate, a.StartedDate, a.CompletedDate))
                    .Distinct()
                    .ToList(),
                g.IsManual))
            .SingleOrDefaultAsync();

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsAgvHeadingToNodeOfType(Guid agvCorrelationId, AgvGraph.NodeType nodeType)
    {
        await using var context = await CreateDbContextAsync();

        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
        IQueryable<EfNode> graphNodes = context.Set<EfNode>();

        return await actionGroups
            .Where(g => g.AssignedAgv == agvCorrelationId && g.State == ActionGroupState.Started)
            .AnyAsync(g => g.Actions.Join(graphNodes, a => a.TargetNodeId,
                n => n.Id, (a, n) => n.Type).Any(b => b == nodeType));
    }
}