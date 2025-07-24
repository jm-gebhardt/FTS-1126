using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

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

        List<ActionGroup> actionGroups = await query
            .Take(maxItems)
            .ToListAsync();

        List<string> targetNodeIds = actionGroups.SelectMany(g => g.Actions).Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!).Distinct().ToList();

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
            g.EstimatedDuration,
            g.State,
            g.Creator,
            g.HandlingUnit,
            g.Actions.Select(a =>
            {
                if (a.Target.NodeId != null)
                {
                    return new AgvActionReadModel(
                        a.CorrelationId,
                        a.ParentId,
                        a.Sequence,
                        a.State,
                        a.ActionType,
                        a.Target.NodeId!,
                        graphNodesById.Count == 0 || !graphNodesById.TryGetValue(a.Target.NodeId!, out _) ?
                            AgvGraph.NodeType.WayPoint : // WayPoint is the default value for not available targetNodeIds
                            graphNodesById[a.Target.NodeId!].Type.GetTypeForActionType(a.ActionType),
                        a.Nodes,
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
                    string.Empty,
                        AgvGraph.NodeType.WayPoint , // WayPoint is the default value for not available targetNodeIds
                    a.Nodes,
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
            g.HasPriority,
            g.IsQueued,
            g.Reasons
        )).ToList();

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> GetCurrentActionGroupsWithNodesAsync(IReadOnlyCollection<Guid> agvCorrelationIds)
    {
        await using var context = await CreateDbContextAsync();
        List<ActionGroup> actionGroups = await context.Set<ActionGroup>()
            .Where(g => g.AssignedAgv != null && agvCorrelationIds.Contains((Guid)g.AssignedAgv) && g.State == ActionGroupState.Started)
            .OrderByDescending(g => g.StartedDate)
            .ToListAsync();

        List<string> targetNodeIds = actionGroups.SelectMany(g => g.Actions.Where(a => a.Target.NodeId != null).Select(a =>  a.Target.NodeId!)).Distinct().ToList();

        List<EfNode> graphNodes = await context.Set<EfNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        List<string> unknownTargetNodeIds = graphNodes.Count == 0 ? targetNodeIds : targetNodeIds.Except(graphNodes.Select(n => n.Id)).ToList();

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
                    actionGroup.EstimatedDuration,
                    actionGroup.State,
                    actionGroup.Creator,
                    actionGroup.HandlingUnit,
                    GetAgvActionReadModel(actionGroup, graphNodes, unknownTargetNodeIds),
                    actionGroup.IsManual,
                    actionGroup.HasPriority,
                    actionGroup.IsQueued,
                    actionGroup.Reasons
                );
            });

        return result;
    }

    private static List<AgvActionReadModel> GetAgvActionReadModel(ActionGroup actionGroup, IReadOnlyCollection<EfNode> graphNodes, IReadOnlyCollection<string> unknownTargetNodeIds)
    {
        var agvActionReadModels1 = new List<AgvActionReadModel>();
        var agvActionReadModels2 = new List<AgvActionReadModel>();
        var agvActionReadModels3 = new List<AgvActionReadModel>();

        if (graphNodes.Count != 0)
        {
            agvActionReadModels1 = actionGroup.Actions.Join(graphNodes, a => a.Target.NodeId, n => n.Id, (a, n) =>
                    new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                        a.Target.NodeId ?? string.Empty, n.Type.GetTypeForActionType(a.ActionType), a.Nodes, a.Edges, a.CreatedDate, a.StartedDate, a.CompletedDate, a.PausedDate, a.PriorRunAndPauseDuration, a.ShouldSkip, a.InfoToAgv, a.AdditionalInfo))
                .Distinct().ToList();

            if (unknownTargetNodeIds.Count == 0)
            {
                return agvActionReadModels1.ToList();
            }
        }

        if (unknownTargetNodeIds.Count != 0)
        {
            agvActionReadModels2 = actionGroup.Actions.Join(unknownTargetNodeIds, a => a.Target.NodeId, u => u, (a, _) =>
                new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                    a.Target.NodeId ?? string.Empty, AgvGraph.NodeType.WayPoint, a.Nodes, a.Edges, a.CreatedDate, a.StartedDate, a.CompletedDate, a.PausedDate, a.PriorRunAndPauseDuration, a.ShouldSkip, a.InfoToAgv, a.AdditionalInfo)).Distinct().ToList();
            if (graphNodes.Count == 0)
            {
                return agvActionReadModels2.ToList();
            }
        }

        if (unknownTargetNodeIds.Count == 0 && graphNodes.Count == 0)
        {
            agvActionReadModels3 = actionGroup.Actions.Select(a => new AgvActionReadModel(a.CorrelationId,
                a.ParentId,
                a.Sequence,
                a.State,
                a.ActionType,
                string.Empty,
                AgvGraph.NodeType.WayPoint,
                a.Nodes,
                a.Edges,
                a.CreatedDate,
                a.StartedDate,
                a.CompletedDate,
                a.PausedDate,
                a.PriorRunAndPauseDuration,
                a.ShouldSkip,
                a.InfoToAgv,
                a.AdditionalInfo)).ToList();
        }

        return agvActionReadModels2.Union(agvActionReadModels1).Union(agvActionReadModels3).ToList();
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel> EnrichActionGroupWithNodesAsync(ActionGroup actionGroup)
    {
        await using var context = await CreateDbContextAsync();

        List<string> targetNodeIds = actionGroup.Actions.Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!).Distinct().ToList();

        List<EfNode> graphNodes = await context.Set<EfNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        List<string> unknownTargetNodeIds = graphNodes.Count == 0 ? targetNodeIds : targetNodeIds.Except(graphNodes.Select(n => n.Id)).ToList();

        var result = new ActionGroupReadModel(actionGroup.CorrelationId,
            actionGroup.OrderId,
            actionGroup.AssignedAgv,
            actionGroup.CreatedDate,
            actionGroup.StartedDate,
            actionGroup.CompletedDate,
            actionGroup.EstimatedDuration,
            actionGroup.State,
            actionGroup.Creator,
            actionGroup.HandlingUnit,
            GetAgvActionReadModel(actionGroup, graphNodes, unknownTargetNodeIds),
            actionGroup.IsManual,
            actionGroup.HasPriority,
            actionGroup.IsQueued,
            actionGroup.Reasons
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

        List<string> targetNodeIds = actionGroup.Actions.Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!).Distinct().ToList();

        List<EfNode> graphNodes = await context.Set<EfNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        List<string> unknownTargetNodeIds = graphNodes.Count == 0 ? targetNodeIds : targetNodeIds.Except(graphNodes.Select(n => n.Id)).ToList();

        return new ActionGroupReadModel(actionGroup.CorrelationId,
            actionGroup.OrderId,
            actionGroup.AssignedAgv,
            actionGroup.CreatedDate,
            actionGroup.StartedDate,
            actionGroup.CompletedDate,
            actionGroup.EstimatedDuration,
            actionGroup.State,
            actionGroup.Creator,
            actionGroup.HandlingUnit,
            GetAgvActionReadModel(actionGroup, graphNodes, unknownTargetNodeIds),
            actionGroup.IsManual,
            actionGroup.HasPriority,
            actionGroup.IsQueued,
            actionGroup.Reasons
        );
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel?> GetCurrentAgvActionGroupWithNodesAsync(Guid agvCorrelationId)
    {
        await using var context = await CreateDbContextAsync();

        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
        var actionGroup = actionGroups.FirstOrDefault(g => g.AssignedAgv == agvCorrelationId && g.State == ActionGroupState.Started);
        if (actionGroup == null)
        {
            return null;
        }

        List<string> targetNodeIds = actionGroups.Where(g => g.AssignedAgv == agvCorrelationId).SelectMany(g => g.Actions).Where(a => a.Target.NodeId != null).Select(a => a.Target.NodeId!).Distinct().ToList();

        List<EfNode> graphNodes = await context.Set<EfNode>()
            .Where(n => targetNodeIds.Contains(n.Id))
            .ToListAsync();

        List<string> unknownTargetNodeIds = graphNodes.Count == 0 ? targetNodeIds : targetNodeIds.Except(graphNodes.Select(n => n.Id)).ToList();

        return new ActionGroupReadModel(actionGroup.CorrelationId,
            actionGroup.OrderId,
            actionGroup.AssignedAgv,
            actionGroup.CreatedDate,
            actionGroup.StartedDate,
            actionGroup.CompletedDate,
            actionGroup.EstimatedDuration,
            actionGroup.State,
            actionGroup.Creator,
            actionGroup.HandlingUnit,
            GetAgvActionReadModel(actionGroup, graphNodes, unknownTargetNodeIds),
            actionGroup.IsManual,
            actionGroup.HasPriority,
            actionGroup.IsQueued,
            actionGroup.Reasons
        );
    }

    /// <inheritdoc />
    public async Task<bool> IsAgvHeadingToNodeOfType(Guid agvCorrelationId, AgvGraph.NodeType nodeType)
    {
        await using var context = await CreateDbContextAsync();

        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
        IQueryable<EfNode> graphNodes = context.Set<EfNode>();

        List<ActionGroup> actionGroupsResult = await actionGroups
            .Where(g => g.AssignedAgv == agvCorrelationId && g.State == ActionGroupState.Started)
            .Include(g => g.Actions)
            .ThenInclude(a => a.Target)
            .ToListAsync();

        return actionGroupsResult.Any(g => g.Actions.Join(
            graphNodes.AsEnumerable(),
            a => a.Target.NodeId,
            n => n.Id,
            (_, n) => n
        ).Any(n => n.Type.IsOfType(nodeType)));
    }
}