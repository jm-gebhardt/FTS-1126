using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

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

    /// <inheritdoc />
    public async Task<List<ActionGroupReadModel>> GetActionGroupsWithTargetNodeTypeAsync(IList<ActionGroupState> states, DateTime? fromDate, DateTime? toDate, int maxItems)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .OrderByDescending(g => g.CreatedDate);
        IQueryable<EfNode> graphNodes = context.Set<EfNode>();

        if (states.Any())
        {
            actionGroups = actionGroups.Where(g => states.Contains(g.State));
        }

        if (fromDate != null && fromDate != DateTime.MinValue)
        {
            actionGroups = actionGroups.Where(g => g.CreatedDate >= fromDate);
        }

        if (toDate != null && toDate != DateTime.MaxValue)
        {
            actionGroups = actionGroups.Where(g => g.CreatedDate <= toDate);
        }

        return (await actionGroups
                .OrderBy(g => g.CreatedDate)
                .ThenByDescending(g => g.IsManual)
                .Include(g => g.Actions)
                .AsNoTracking()
                .ToListAsync())
            .Select(g => new ActionGroupReadModel(g.CorrelationId,
                g.OrderId, g.AssignedAgv, g.CreatedDate, g.StartedDate, g.CompletedDate, g.State, g.Creator,
                g.Actions.Join(graphNodes, a => a.TargetNodeId, n => n.Id, (a, n) =>
                        new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                            a.TargetNodeId, n.Type, a.Nodes, a.CreatedDate, a.StartedDate, a.CompletedDate))
                    .Distinct()
                    .ToList(), g.IsManual))
            .Take(maxItems).ToList();
    }

  

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ActionGroupReadModel?>> GetCurrentAssignedActionGroupsWithTargetNodeTypeAsync(IEnumerable<Guid> agvCorrelationIds)
    {
        // Once Agv is assigned to ActionGroup, Status is immediately set to started (and StartedDate), therefore we can use StartedDate to get current assigned ActionGroup  
        List<Task<(Guid agvCorrelationId, ActionGroupReadModel? actionGroup)>> tasks = agvCorrelationIds
            .Select(async agvCorrelationId =>
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync();
                IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
                IQueryable<EfNode> graphNodes = context.Set<EfNode>();
                var actionGroup = (await actionGroups.OrderByDescending(g => g.StartedDate)
                    .Include(g => g.Actions)
                    .ToListAsync())
                    .Select(g => new ActionGroupReadModel(g.CorrelationId,
                        g.OrderId, g.AssignedAgv, g.CreatedDate, g.StartedDate, g.CompletedDate, g.State, g.Creator,
                        g.Actions.Join(graphNodes, a => a.TargetNodeId, n => n.Id, (a, n) =>
                                new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                                    a.TargetNodeId, n.Type, a.Nodes, a.CreatedDate, a.StartedDate, a.CompletedDate))
                            .Distinct()
                            .ToList(), g.IsManual))
                    .FirstOrDefault(a => a.AssignedAgv == agvCorrelationId);

                return (agvCorrelationId, actionGroup);
            }).ToList();
        await Task.WhenAll(tasks);

        Dictionary<Guid, ActionGroupReadModel?> actions = tasks.ToDictionary(t => t.Result.agvCorrelationId, t => t.Result.actionGroup);

        return actions;
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel> EnrichActionGroupWithTargetNodeTypesAsync(ActionGroup actionGroup)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<EfNode> graphNodes = context.Set<EfNode>();
        return  new ActionGroupReadModel(actionGroup.CorrelationId,
            actionGroup.OrderId, actionGroup.AssignedAgv, actionGroup.CreatedDate, actionGroup.StartedDate, actionGroup.CompletedDate, actionGroup.State,
            actionGroup.Creator,
            actionGroup.Actions.Join(graphNodes, a => a.TargetNodeId, n => n.Id, (a, n) =>
                    new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                        a.TargetNodeId, n.Type, a.Nodes, a.CreatedDate, a.StartedDate, a.CompletedDate))
                .Distinct()
                .ToList(), actionGroup.IsManual);
    }

    /// <inheritdoc />
    public async Task<ActionGroupReadModel?> GetActionGroupWithTargetNodeTypesAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();
        IQueryable<EfNode> graphNodes = context.Set<EfNode>();
        return (await actionGroups.Where(g => g.CorrelationId == correlationId)
                .Include(g => g.Actions)
                .ToListAsync())
            .Select(g => new ActionGroupReadModel(g.CorrelationId,
                g.OrderId, g.AssignedAgv, g.CreatedDate, g.StartedDate, g.CompletedDate, g.State, g.Creator,
                g.Actions.Join(graphNodes, a => a.TargetNodeId, n => n.Id, (a, n) =>
                        new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State, a.ActionType,
                            a.TargetNodeId, n.Type, a.Nodes, a.CreatedDate, a.StartedDate, a.CompletedDate))
                    .Distinct()
                    .ToList(), g.IsManual))
            .SingleOrDefault();
    }
}