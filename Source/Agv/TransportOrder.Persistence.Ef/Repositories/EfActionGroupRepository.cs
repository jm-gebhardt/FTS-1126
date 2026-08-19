using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using JetBrains.Annotations;
using MassTransit.Initializers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Repository to access ActionGroups
/// </summary>
[PublicAPI]
[CanBeCaptiveDependency]
public class EfActionGroupRepository : IActionGroupRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly ILogger<EfActionGroupRepository> _logger;

    /// <inheritdoc cref="EfActionGroupRepository"/>
    public EfActionGroupRepository(ILogger<EfActionGroupRepository> logger, IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// GraphNode is just a reference and always existing.
    /// </summary>
    /// <param name="actionGroup"></param>
    private static Dictionary<Guid, GraphNode> StripNodes(ActionGroup actionGroup)
    {
        Dictionary<Guid, GraphNode> nodesById = [];
        foreach (var action in actionGroup.Actions)
        {
            if (action.Target.NodeId != null && !nodesById.ContainsKey(action.Target.NodeId.Value))
            {
                nodesById[action.Target.NodeId.Value] = action.Target.Node!;
            }
            action.Target.Node = null;
            foreach (var actionNode in action.AgvActionNodes)
            {
                if (!nodesById.ContainsKey(actionNode.NodeId))
                {
                    nodesById[actionNode.NodeId] = actionNode.Node;
                }
                actionNode.Node = null!;
            }
        }

        return nodesById;
    }

    private static void RestoreNodes(ActionGroup actionGroup, Dictionary<Guid, GraphNode> nodesById)
    {
        foreach (var action in actionGroup.Actions)
        {
            if (action.Target.NodeId != null && nodesById.TryGetValue(action.Target.NodeId.Value, out var actionTargetNode))
            {
                action.Target.Node = actionTargetNode;
            }
            foreach (var actionNode in action.AgvActionNodes)
            {
                if (nodesById.TryGetValue(actionNode.NodeId, out var actionNodeNode))
                {
                    actionNode.Node = actionNodeNode;
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> AddAsync(ActionGroup actionGroup)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        Dictionary<Guid, GraphNode> savedNodes = StripNodes(actionGroup);
        await context.AddAsync(actionGroup);
        UpdateComputedProperties(context);
        bool changes = await context.SaveChangesAsync() > 0;
        RestoreNodes(actionGroup, savedNodes);
        return changes;
    }

    /// <inheritdoc />
    public async Task<bool> AddOrUpdateAsync(ActionGroup actionGroup)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        Dictionary<Guid, GraphNode> savedNodes = StripNodes(actionGroup);
        if (await context.Set<ActionGroup>().AnyAsync(a => a.CorrelationId == actionGroup.CorrelationId))
        {
            await UpdateActionGroupAndActions(actionGroup, context);
            RestoreNodes(actionGroup, savedNodes);
            return true;
        }
        await ClearStaleTargetNodeIdsAsync(actionGroup, context);
        await context.AddAsync(actionGroup);
        UpdateComputedProperties(context);
        await context.SaveChangesAsync();
        RestoreNodes(actionGroup, savedNodes);
        return false;
    }

    private async Task ClearStaleTargetNodeIdsAsync(ActionGroup actionGroup, TransportOrderDbContext context)
    {
        List<Guid> referencedNodeIds = actionGroup.Actions
            .Where(a => a.Target.NodeId != null)
            .Select(a => a.Target.NodeId!.Value)
            .Distinct()
            .ToList();

        if (referencedNodeIds.Count == 0)
        {
            return;
        }

        HashSet<Guid> existingNodeIds = (await context.Set<GraphNode>()
            .Where(n => referencedNodeIds.Contains(n.Id))
            .Select(n => n.Id)
            .ToListAsync()).ToHashSet();

        foreach (var action in actionGroup.Actions)
        {
            if (action.Target.NodeId != null && !existingNodeIds.Contains(action.Target.NodeId.Value))
            {
                _logger.LogWarning(
                    "Clearing stale Target.NodeId {NodeId} on action {ActionId} before insert — node no longer exists",
                    action.Target.NodeId, action.CorrelationId);
                action.Target.NodeId = null;
            }
        }
    }

    private async Task UpdateActionGroupAndActions(ActionGroup actionGroup, TransportOrderDbContext context)
    {
        var existingActionGroup = await context.Set<ActionGroup>().Where(a => a.CorrelationId == actionGroup.CorrelationId).Include(a => a.Actions).AsNoTracking().SingleAsync();
        List<Guid> existingActions = existingActionGroup.Actions.Select(a => a.CorrelationId).ToList();
        List<Guid> currentActions = actionGroup.Actions.Select(a => a.CorrelationId).ToList();
        foreach (var action in actionGroup.Actions)
        {
            context.Entry(action).State = EntityState.Detached;
        }

        context.Entry(actionGroup).State = EntityState.Modified;
        context.Set<ActionGroup>().Update(actionGroup);
        List<AgvAction> actionsToAdd = actionGroup.Actions.Where(a => !existingActions.Contains(a.CorrelationId)).ToList();
        List<AgvAction> actionsToRemove = existingActionGroup.Actions.Where(a => !currentActions.Contains(a.CorrelationId)).ToList();
        if (actionsToAdd.Count > 0)
        {
            context.AddRange(actionsToAdd);
        }
        if (actionsToRemove.Count > 0)
        {
            context.RemoveRange(actionsToRemove);
        }
        List<AgvAction> actionsToUpdate = actionGroup.Actions.Where(a =>
            existingActions.Contains(a.CorrelationId) &&
            !a.Equals(existingActionGroup.Actions.SingleOrDefault(e => e.CorrelationId == a.CorrelationId))).ToList();
        if (actionsToUpdate.Count > 0)
        {
            context.UpdateRange(actionsToUpdate);
        }
        UpdateComputedProperties(context);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Update any computed properties that are meant to be written into the database here explicitly
    /// (e.g. for allowing to use them in database filters).
    /// </summary>
    /// <param name="context"></param>
    private void UpdateComputedProperties(TransportOrderDbContext context)
    {
        foreach (EntityEntry<ActionGroup> entry in context.ChangeTracker.Entries<ActionGroup>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Property(e => e.Duration).CurrentValue = entry.Entity.Duration;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(ActionGroupState actionGroupState, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 100, Guid? assignedAgvCorrelationId = null, bool? isManual = null)
    {
        return await GetActionGroupsAsync(new List<ActionGroupState> { actionGroupState }, fromDate, toDate, maxItems, assignedAgvCorrelationId, isManual);
    }

    /// <inheritdoc />
    public async Task<ActionGroup?> GetNextManualActionGroupForAgvAsync(Guid assignedAgv, List<ActionType>? allowedActionTypes = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .Where(a => a.AssignedAgv == assignedAgv && a.IsManual && a.IsQueued && a.State == ActionGroupState.Created)
            .AsNoTracking();

        if (allowedActionTypes != null)
        {
            actionGroups = actionGroups.Where(ag => ag.Actions.Any(a => allowedActionTypes.Contains(a.ActionType)));
        }

        actionGroups = actionGroups.OrderByProperty(nameof(SortBy.CreatedDate), isDescending: false).ThenByDescending(g => g.Priority);
        return actionGroups.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Dictionary<ActionType, ICollection<double>>> GetActionDurationsByTypeAsync(ActionGroupState actionGroupState, Guid? sourceNodeId = null, Guid? targetNodeId = null, int maxItems = 1000)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .Where(a => a.State == actionGroupState)
            .AsNoTracking();

        if (sourceNodeId != null && targetNodeId == null)
        {
            actionGroups = actionGroups.Where(a => a.Actions.Any(action => action.Target.NodeId!.Value == sourceNodeId));
        }

        if (sourceNodeId == null && targetNodeId != null)
        {
            actionGroups = actionGroups.Where(a => a.Actions.Any(action => action.Target.NodeId!.Value == targetNodeId));
        }

        if (sourceNodeId != null && targetNodeId != null)
        {
            actionGroups = actionGroups.Where(a =>
                a.Actions.Any(action => action.Target.NodeId!.Value == sourceNodeId) &&
                a.Actions.Any(action => action.Target.NodeId!.Value == targetNodeId));
        }

        List<AgvAction> actions = await actionGroups
            .OrderByDescending(a => a.StartedDate)
            .Reverse()
            .Take(maxItems)
            .SelectMany(a => a.Actions)
            .ToListAsync();

        if (actions.Count > 0)
        {
            return
                actions
                    .GroupBy(a => a.ActionType)
                    .ToDictionary(
                        g => g.Key, ICollection<double> (g) => g.Select(a => a.Duration == null ? 0 : a.Duration!.Value.TotalSeconds).ToList()
                    );
        }

        return new Dictionary<ActionType, ICollection<double>>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(IList<ActionGroupState>? states = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 100, Guid? assignedAgvCorrelationId = null, bool? isManual = null, SortBy sortBy = SortBy.CreatedDate, bool sortDescending = true)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>();

        if (states?.Any() ?? false)
        {
            actionGroups = actionGroups.Where(g => states.Contains(g.State));
        }

        if (isManual != null)
        {
            actionGroups = actionGroups.Where(g => g.IsManual == isManual);
        }

        if (assignedAgvCorrelationId != null)
        {
            actionGroups = actionGroups.Where(g => g.AssignedAgv == assignedAgvCorrelationId);
        }

        if (fromDate != null && fromDate != DateTime.MinValue)
        {
            actionGroups = actionGroups.Where(g => g.CreatedDate >= fromDate);
        }

        if (toDate != null && toDate != DateTime.MaxValue)
        {
            actionGroups = actionGroups.Where(g => g.CreatedDate <= toDate);
        }

        List<ActionGroup> actionGroupsAsync = await actionGroups
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .OrderByProperty(sortBy.ToString(), isDescending: sortDescending)
            .ThenByDescending(g => g.Priority)
            .Take(maxItems)
            .ToListWithNoLockAsync();
        return actionGroupsAsync;
    }

    /// <inheritdoc />
    public async Task<int> GetActionGroupsCountAsync(IEnumerable<ActionGroupState> states, DateTime? fromDate = null, DateTime? toDate = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

        IQueryable<ActionGroup> actionGroups = queryable
            .Where(g => states.Contains(g.State));

        if (fromDate != null)
        {
            actionGroups = actionGroups.Where(g => g.CreatedDate >= fromDate);
        }

        if (toDate != null)
        {
            actionGroups = actionGroups.Where(g => g.CreatedDate <= toDate);
        }

        return await actionGroups.CountAsync();
    }

    /// <inheritdoc />
    public async Task<AgvAction?> GetCurrentAgvActionAsync(Guid agvCorrelationId)
    {
        var actionGroup = await GetCurrentAssignedActionGroupAsync(agvCorrelationId);
        var currentAction = actionGroup?.Actions.OrderByDescending(a => a.Sequence).FirstOrDefault(a => a.State == ActionState.Started);
        return currentAction;
    }

    /// <inheritdoc />
    public async Task<ActionGroup?> GetCurrentAssignedActionGroupAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var actionGroup = await context.Set<ActionGroup>().OrderByDescending(g => g.StartedDate)
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .FirstOrDefaultAsync(a => a.AssignedAgv == agvCorrelationId && a.State == ActionGroupState.Started);

        return actionGroup;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ActionGroup?>> GetCurrentAssignedActionGroupsAsync(IEnumerable<Guid> agvCorrelationIds)
    {
        // Once Agv is assigned to ActionGroup, Status is immediately set to started (and StartedDate), therefore we can use StartedDate to get current assigned ActionGroup  
        List<Task<(Guid agvCorrelationId, ActionGroup? actionGroup)>> tasks = agvCorrelationIds
            .Select(async agvCorrelationId =>
            {
                var actionGroup = await GetCurrentAssignedActionGroupAsync(agvCorrelationId);
                return (agvCorrelationId, actionGroup);
            }).ToList();
        await Task.WhenAll(tasks);

        Dictionary<Guid, ActionGroup?> actions = tasks.ToDictionary(
            t => t.Result.agvCorrelationId,
            t => t.Result.actionGroup);

        return actions;
    }

    /// <inheritdoc />
    public async Task<ActionGroup?> GetActionGroupAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .SingleOrDefaultAsync(a => a.CorrelationId == correlationId);
        return actionGroup;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetHandlingUnitsAsync(string query, int maxItems)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<ActionGroup>()
            .AsNoTracking()
            .Where(ag => ag.HandlingUnit != null && ag.HandlingUnit.Contains(query))
            .Select(a => a.HandlingUnit!)
            .Distinct()
            .OrderBy(s => s)
            .Take(maxItems)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ActionGroup> GetRequiredActionGroupByIdAsync(Guid correlationId)
    {
        var actionGroup = await GetActionGroupAsync(correlationId);
        if (actionGroup == null)
        {
            throw new ActionGroupNotFoundException(correlationId);
        }

        return actionGroup;
    }

    /// <inheritdoc />
    public async Task<ActionGroup> GetRequiredActionGroupByActionIdAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var actionGroupId = await context.Set<ActionGroup>()
            .Where(ag => ag.Actions.Any(a => a.CorrelationId == correlationId))
            .Select(ag => ag.CorrelationId)
            .SingleOrDefaultAsync();

        if (actionGroupId == Guid.Empty)
        {
            throw new ActionGroupNotFoundException(correlationId);
        }

        var actionGroup = await context.Set<ActionGroup>()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(an => an.Node)
            .SingleAsync(g => g.CorrelationId == actionGroupId);

        return actionGroup ?? throw new ActionGroupNotFoundException(correlationId);
    }

    /// <inheritdoc />
    public async Task<ICollection<ActionGroup>> GetRunningIdleActionGroups()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return (await context.Set<ActionGroup>()
                .Include(g => g.Actions)
                .ThenInclude(a => a.AgvActionNodes)
                .ThenInclude(a => a.Node)
                .Where(a => a.State == ActionGroupState.Started && !a.IsManual && a.Priority != Priority.QueueEvacuation && a.Priority != Priority.Queueing).ToListAsync())
            .Where(a => !a.IsMovingCargo).ToList();
    }

    /// <inheritdoc />
    public async Task<ICollection<ActionGroup>> GetRunningChargeActionGroups()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<ActionGroup>()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .Where(ag => ag.State == ActionGroupState.Started && ag.Actions.Any(a =>
                a.ActionType == ActionType.Charge && a.State == ActionState.Started)).ToListAsync();
    }

    /// <inheritdoc cref="IActionGroupRepository"/>
    public async Task<AgvAction?> GetActionByIdAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

        var action = await queryable
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .SingleOrDefaultAsync(actionGroup => actionGroup.Actions.Any(a => a.CorrelationId == correlationId))
            .Select(ag => ag?.Actions.SingleOrDefault(a => a.CorrelationId == correlationId));

        return action;
    }

    /// <inheritdoc cref="IActionGroupRepository"/>
    public async Task<AgvAction> GetRequiredActionByIdAsync(Guid correlationId)
    {
        var action = await GetActionByIdAsync(correlationId);
        if (action != null)
        {
            return action;
        }

        throw new ActionNotFoundException("ActionCorrelationId", nameof(correlationId), correlationId);
    }

    /// <inheritdoc />
    public async Task<ActionGroup> UpdateActionAndGroupStateAsync(AgvAction action, Agv? agv, ActionState state, Reason? reason, DateTimeOffset now)
    {
        var actionGroupCorrelationId = action.ParentId;

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable
            .Include(a => a.Actions)
            .SingleOrDefaultAsync(a => a.CorrelationId == actionGroupCorrelationId);

        if (actionGroup == null)
        {
            throw new ActionGroupNotFoundException(actionGroupCorrelationId);
        }

        action.UpdateActionState(state, now);

        var actionToUpdate = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId);
        context.Entry(actionToUpdate).CurrentValues.SetValues(action);
        if (agv != null && actionGroup.AssignedAgv == null)
        {
            actionGroup.Assign(agv.CorrelationId);
        }

        actionGroup.UpdateActionGroupState();

        if (reason != null)
        {
            actionGroup.Reasons.Add(reason);
        }
        await context.SaveChangesAsync();

        return actionGroup;
    }

    /// <inheritdoc/>
    public async Task<bool> HasUnfinishedDropActionsAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        bool hasUnfinishedDropActions = await queryable.Where(actionGroup =>
                actionGroup.AssignedAgv == agvCorrelationId &&
                (actionGroup.State == ActionGroupState.Started || actionGroup.State == ActionGroupState.Paused) &&
                actionGroup.Actions.Any(a => a.State != ActionState.Completed && a.ActionType == ActionType.Drop)
            )
            .AsNoTracking()
            .AsSplitQuery()
            .AnyWithNoLockAsync();

        return hasUnfinishedDropActions;
    }

    /// <inheritdoc/>
    public async Task<List<ActionGroup>> GetNotStartedActionsAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        List<ActionGroup> getNotStartedActions = await queryable.Where(actionGroup =>
                actionGroup.AssignedAgv == agvCorrelationId &&
                actionGroup.IsQueued &&
                (actionGroup.State == ActionGroupState.Created || (actionGroup.State == ActionGroupState.Started && actionGroup.Actions.Any(aa => aa.State == ActionState.Created))) &&
                actionGroup.Actions.Any(a => a.State != ActionState.Completed && a.State != ActionState.Started)
            )
            .AsNoTracking()
            .AsSplitQuery()
            .ToListWithNoLockAsync();

        return getNotStartedActions;
    }

    /// <inheritdoc/>
    public async Task<bool> HasNotStartedActionsAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        bool hasNotStartedActions = await queryable.Where(actionGroup =>
                actionGroup.AssignedAgv == agvCorrelationId &&
                actionGroup.IsQueued &&
                (actionGroup.State == ActionGroupState.Created || (actionGroup.State == ActionGroupState.Started && actionGroup.Actions.Any(aa => aa.State == ActionState.Created))) &&
                actionGroup.Actions.Any(a => a.State != ActionState.Completed && a.State != ActionState.Started)
            )
            .AsNoTracking()
            .AnyWithNoLockAsync();

        return hasNotStartedActions;
    }

    /// <inheritdoc />
    public async Task<bool> HasUnfinishedActionGroupAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        bool hasNotStartedActions = await queryable.Where(actionGroup =>
                actionGroup.AssignedAgv == agvCorrelationId &&
                actionGroup.State == ActionGroupState.Started &&
                actionGroup.Actions.Any(a => a.State != ActionState.Completed && a.State != ActionState.Started)
            )
            .AsNoTracking()
            .AnyAsync();

        return hasNotStartedActions;
    }

    /// <inheritdoc />
    public async Task<bool> HasCurrentAssignedActionGroupAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        bool hasActions = await queryable.Where(actionGroup =>
                actionGroup.AssignedAgv == agvCorrelationId &&
                actionGroup.IsQueued &&
                (actionGroup.State == ActionGroupState.Started || actionGroup.State == ActionGroupState.Created)
            )
            .AsNoTracking()
            .AnyAsync();

        return hasActions;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ActionGroup>> GetNotStartedActionGroupsAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        List<ActionGroup> notStartedActionGroups = await queryable
            .Where(actionGroup =>
                actionGroup.State == ActionGroupState.Created && actionGroup.IsQueued
                                                              && (actionGroup.AssignedAgv == null || actionGroup.AssignedAgv == agvCorrelationId)
            )
            .OrderByDescending(g => g.Priority)
            .ThenBy(g => g.CreatedDate)
            .AsNoTracking()
            .ToListWithNoLockAsync();

        return notStartedActionGroups;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ActionGroup>> GetInvalidActionGroupsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        List<ActionGroup> notStartedActionGroups = await queryable
            .Where(actionGroup => actionGroup.State == ActionGroupState.Invalid)
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .ToListAsync();

        return notStartedActionGroups;
    }

    /// <inheritdoc/>
    public async Task<AgvAction> GetSubsequentActionAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var startedGroup = await queryable
            .Where(actionGroup => actionGroup.AssignedAgv == agvCorrelationId &&
                                  actionGroup.State == ActionGroupState.Started &&
                                  actionGroup.Actions.Any(a => a.State != ActionState.Completed))
            .OrderBy(a => a.StartedDate)
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .FirstOrDefaultAsync();

        var subsequentAction = startedGroup?.Actions.Where(action => action.State != ActionState.Completed && !action.ShouldSkip).MinBy(a => a.Sequence);

        if (subsequentAction == null)
        {
            throw new ActionNotFoundException(nameof(subsequentAction), nameof(agvCorrelationId), agvCorrelationId);
        }
        return subsequentAction;
    }

    /// <inheritdoc />
    public async Task<bool> AgvHasPendingActions(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        DbSet<ActionGroup> actionGroups = context.Set<ActionGroup>();

        return await actionGroups.AnyAsync(a =>
            (a.AssignedAgv == agvCorrelationId && a.IsQueued &&
             (a.State == ActionGroupState.Created || (a.State == ActionGroupState.Started && a.Actions.Any(aa => aa.State == ActionState.Created)))) ||
            (a.AssignedAgv == null && a.State == ActionGroupState.Created && a.IsQueued)
        );
    }

    /// <inheritdoc />
    public async Task<Guid?> GetAgvAssignedToAction(Guid actionCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        DbSet<ActionGroup> actionGroups = context.Set<ActionGroup>();
        return (await actionGroups.SingleAsync(ag => ag.Actions.Any(a => a.CorrelationId == actionCorrelationId))).AssignedAgv;
    }

    /// <inheritdoc />
    public async Task<bool> HasStartedActionAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        DbSet<ActionGroup> actionGroups = context.Set<ActionGroup>();
        return await actionGroups.AnyAsync(ag => ag.State == ActionGroupState.Started && ag.AssignedAgv == agvCorrelationId && ag.Actions.Any(a => a.State == ActionState.Started));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ActionGroup actionGroup)
    {
        Dictionary<Guid, GraphNode> savedNodes = StripNodes(actionGroup);
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Set<ActionGroup>().Update(actionGroup);
        UpdateComputedProperties(context);
        int result = await context.SaveChangesAsync();
        _logger.LogDebug("Update action group async save affected {RowsAffected} rows", result);
        await context.Entry(actionGroup).ReloadAsync();
        RestoreNodes(actionGroup, savedNodes);
        _logger.LogDebug("Action group {ActionGroupCorrelationId} state after saving update {State}", actionGroup.CorrelationId, actionGroup.State);
        foreach (var action in actionGroup.Actions)
        {
            _logger.LogDebug("Action {ActionType} of group {ActionGroupCorrelationId} has DoppelspielPartner {Partner} and is registered {IsRegistered}", action.ActionType, actionGroup.CorrelationId, action.DoppelspielPartner, action.IsRegisteredForDoppelspiel);
        }
    }

    /// <inheritdoc />
    public async Task<double> GetAverageDurationEstimateDeviationAsync(Guid agvCorrelationId, Guid[] targetNodes)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        try
        {
            // Use average assuming extreme outliers are rare.
            // There is no median db operation and client side median calculations can be memory intense for large numbers
            // of action groups.
            DbSet<ActionGroup> actionGroups = context.Set<ActionGroup>();
            // Cast to IEnumerable to avoid C# 14 span overload resolution breaking change
            IEnumerable<Guid> nodeIds = targetNodes;

            IQueryable<ActionGroup> queryable = actionGroups.Where(ag =>
                ag.StartedDate.HasValue && ag.CompletedDate.HasValue && ag.EstimatedDuration.HasValue &&
                ag.AssignedAgv == agvCorrelationId &&
                ag.Actions.All(a => a.Target.NodeId.HasValue) &&
                ag.Actions.All(a => nodeIds.Contains(a.Target.NodeId!.Value)));

            if (!queryable.Any())
            {
                return 0;
            }
            return queryable
                .Average(ag => (ag.CompletedDate!.Value - ag.StartedDate!.Value - ag.EstimatedDuration!.Value).TotalSeconds);
        }
        catch (InvalidOperationException)
        {
            // no action groups found that match the query
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsLastCompletedActionMoveToCoordinate(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var action = await context.Set<ActionGroup>()
            .AsNoTracking()
            .Where(ag => ag.AssignedAgv == agvCorrelationId && ag.Actions.Any(a => a.CompletedDate != null))
            .SelectMany(a => a.Actions)
            .Where(a => a.CompletedDate != null)
            .OrderByDescending(a => a.CompletedDate)
            .FirstOrDefaultAsync();
        return action is { IsMoveToCoordinate: true };
    }

    /// <inheritdoc/>
    public async Task<bool?> GetDockFrontOfLastCompletedAction(Guid agvCorrelationId, Guid actionGroupCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable
            .Include(a => a.Actions)
            .Where(a => a.CorrelationId != actionGroupCorrelationId && a.AssignedAgv == agvCorrelationId && (a.State == ActionGroupState.Completed || a.State == ActionGroupState.Started))
            .OrderByDescending(a => a.StartedDate)
            .FirstOrDefaultAsync();
        bool? dockFront = actionGroup?.Actions.OrderByDescending(a => a.Sequence).First(a => !a.ShouldSkip).InfoToAgv?.GetDockFront();
        return dockFront;
    }

    /// <inheritdoc/>
    public async Task<AgvAction?> GetLastAbortedPickDropActionAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable
            .Include(ag => ag.Actions)
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .Where(ag => ag.AssignedAgv == agvCorrelationId && ag.Actions.Any(a => a.ActionType == ActionType.Pick || a.ActionType == ActionType.Drop))
            .OrderByDescending(ag => ag.StartedDate)
            .ThenByDescending(ag => ag.CompletedDate)
            .FirstOrDefaultAsync(ag => ag.Actions.Any(a => a.State == ActionState.Aborted));

        var action = actionGroup?.Actions.OrderByDescending(a => a.CompletedDate).FirstOrDefault(a => a.CompletedDate != null);
        return action;
    }

    /// <inheritdoc/>
    public async Task<bool> HasCreatedChargingActionGroupForAgvAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Actions are auto-included
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking();

        // The SQL created is less than optimal. If this becomes problematic, 
        // may want to create a stored procedure and call that instead.
        return await actionGroups.Where(ag =>
                ag.AssignedAgv == agvCorrelationId
                && ag.State == ActionGroupState.Created
                && ag.IsQueued
                && ag.Actions.Any(a =>
                    a.ActionType == ActionType.Charge
                    && a.State == ActionState.Created))
            .AnyAsync();
    }

    /// <inheritdoc/>
    public async Task<ActionGroup?> GetStartedActionGroupForAgvAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Actions are auto-included.
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node);

        return await actionGroups
            .OrderByDescending(ag => ag.StartedDate)
            .FirstOrDefaultAsync(ag =>
                ag.AssignedAgv == agvCorrelationId
                && ag.State == ActionGroupState.Started);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ActionGroup>> GetCreatedAndStartedActionGroupsForAgvAsync(Guid agvCorrelationId)
    {
        var actionGroupStates = new List<ActionGroupState>()
        {
            ActionGroupState.Created,
            ActionGroupState.Started
        };

        return (await GetAgvActionGroupsInState(agvCorrelationId, actionGroupStates)).Where(ag => ag.IsQueued).ToList();
    }

    /// <inheritdoc/>
    public async Task<ICollection<ActionGroup>> GetPendingActionGroupsForAgvAsync(Guid agvCorrelationId)
    {
        var actionGroupStates = new List<ActionGroupState>()
        {
            ActionGroupState.Created,
            ActionGroupState.Started,
            ActionGroupState.Paused
        };

        return (await GetAgvActionGroupsInState(agvCorrelationId, actionGroupStates)).Where(ag => ag.IsQueued).ToList();
    }

    /// <inheritdoc/>
    public async Task<ICollection<Guid>> FilterAgvsWithPriorityActionGroups(ICollection<Guid> agvs)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking();

        return (await actionGroups.Where(a => a.Priority > Priority.Default && a.Priority != Priority.Charging && a.IsQueued &&
                                              a.AssignedAgv != null && agvs.Contains(a.AssignedAgv!.Value) &&
                                              (a.State == ActionGroupState.Started || a.State == ActionGroupState.Created))
            .ToListAsync()).Select(a => a.AssignedAgv!.Value).ToList();
    }

    /// <inheritdoc/>
    public async Task<ICollection<ActionGroup>> GetCurrentActionGroupsToTarget(Guid targetId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node);
        return await actionGroups.Where(ag =>
                (ag.State == ActionGroupState.Started || ag.State == ActionGroupState.Created) &&
                ag.Actions.Any(a => (a.State == ActionState.Started || a.State == ActionState.Created) && a.Target.NodeId!.Value == targetId))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<ActionGroup>> GetNextPicksAtTarget(Guid targetId, ICollection<Guid> assignedAgvs)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node);
        return await actionGroups.Where(ag =>
                !ag.IsQueued &&
                ag.State == ActionGroupState.Created &&
                ag.AssignedAgv != null &&
                assignedAgvs.Contains(ag.AssignedAgv.Value) &&
                ag.Actions.Any(a => a.ActionType == ActionType.Pick && a.Target.NodeId!.Value == targetId))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ActionGroup?> GetNextDropActionGroupEligibleForDoppelspiel(ICollection<string> stations)
    {
        IEnumerable<string> targets = stations;
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<ActionGroup> candidates = await context.Set<ActionGroup>()
            .Where(o => o.CanRunDoppelspiel
                        && o.CompletedDate == null
                        && o.IsQueued
                        && o.Actions.Any(a => a.ActionType == ActionType.Drop)
                        && !o.Actions.Any(a => a.ActionType == ActionType.Drop && a.IsRegisteredForDoppelspiel)
                        && o.Actions.Any(a => a.ActionType == ActionType.Pick && a.CompletedDate != null))
            .ToListAsync();

        return candidates.Where(o =>
                o.Actions.Any(a => a.ActionType == ActionType.Drop &&
                                   ((a.Target.Node != null && targets.Contains(a.Target.Node.Name)) ||
                                    targets.All(t => a.Target.StationOptions.Contains(t)))))
            .OrderBy(o => o.StartedDate)
            .ThenBy(o => o.ConsideredForDoppelspielDate).FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<ActionGroup?> GetNextDelayedDropForStations(ICollection<Guid> stations)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<ActionGroup>()
            .Where(a => !a.IsQueued
                        && a.CompletedDate == null
                        && a.StartedDate == null
                        && a.Actions.Any(ac => ac.ActionType == ActionType.Drop && ac.Target.Node != null && stations.Contains(ac.Target.Node.Id)))
            .OrderBy(a => a.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<ActionGroup?> GetNextPickForStation(GraphNode pickNode, ICollection<Guid>? ordersToIgnore = null)
    {
        ordersToIgnore ??= [];
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        List<ActionGroup> candidates = await context.Set<ActionGroup>()
            .Where(a => !a.IsQueued
                        && !ordersToIgnore.Contains(a.CorrelationId)
                        && a.CompletedDate == null
                        && a.StartedDate == null
                        && a.Actions.Any(ac => ac.ActionType == ActionType.Pick))
            .OrderBy(h => h.CreatedDate)
            .ToListAsync();

        return candidates.FirstOrDefault(a =>
            a.Actions.Any(ac => ac is { ActionType: ActionType.Pick, DoppelspielPartner: null } &&
                                ((ac.Target.Node != null && ac.Target.Node.Id == pickNode.Id) ||
                                 ac.Target.StationOptions.Contains(pickNode.Name))));
    }

    /// <inheritdoc/>
    public async Task<ActionGroup?> GetNextUnmatchedDoppelspielDrop(GraphNode node, Guid? orderToIgnore)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Step 1: Fetch pick candidates from database (without StationOptions check)
        List<ActionGroup> pickCandidates = await context.Set<ActionGroup>()
            .Where(a => !a.IsQueued &&
                        a.CorrelationId != orderToIgnore &&
                        a.CompletedDate == null &&
                        a.StartedDate == null &&
                        a.Actions.Any(ac => ac.ActionType == ActionType.Pick &&
                                            ac.IsRegisteredForDoppelspiel &&
                                            ac.DoppelspielPartner == null))
            .OrderBy(h => h.CreatedDate)
            .ToListAsync();

        // Step 2: Filter in memory with StationOptions check
        int pickCount = pickCandidates.Count(a =>
            a.Actions.Any(ac => ac.ActionType == ActionType.Pick &&
                                ((ac.Target.Node != null && ac.Target.Node.Id == node.Id) ||
                                 ac.Target.StationOptions.Contains(node.Name)) &&
                                ac is { IsRegisteredForDoppelspiel: true, DoppelspielPartner: null }));

        // Step 3: Fetch drop candidates from database (without StationOptions check)
        List<ActionGroup> dropCandidates = await context.Set<ActionGroup>()
            .Where(a => a.CanRunDoppelspiel &&
                        a.CompletedDate == null &&
                        a.Actions.Any(ac => ac.ActionType == ActionType.Drop &&
                                            ac.IsRegisteredForDoppelspiel &&
                                            ac.DoppelspielPartner == null))
            .OrderBy(h => h.ConsideredForDoppelspielDate)
            .ToListAsync();

        // Step 4: Filter in memory with StationOptions check
        List<ActionGroup> dropOrders = dropCandidates
            .Where(a => a.Actions.Any(ac => ac.ActionType == ActionType.Drop &&
                                            ((ac.Target.Node != null && ac.Target.Node.Id == node.Id) ||
                                             ac.Target.StationOptions.Contains(node.Name)) &&
                                            ac is { IsRegisteredForDoppelspiel: true, DoppelspielPartner: null }))
            .ToList();

        if (pickCount >= dropOrders.Count)
        {
            // All drops will be part of a Doppelspiel
            return null;
        }

        return dropOrders.Skip(pickCount).FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<ActionGroup?> GetNextUnmatchedDoppelspielPick(GraphNode node, Guid? orderToIgnore)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Step 1: Fetch pick candidates from database (without StationOptions check)
        List<ActionGroup> pickCandidates = await context.Set<ActionGroup>()
            .Where(a => !a.IsQueued
                        && a.CompletedDate == null
                        && a.StartedDate == null
                        && a.Actions.Any(ac => ac.ActionType == ActionType.Pick &&
                                               ac.IsRegisteredForDoppelspiel &&
                                               ac.DoppelspielPartner == null &&
                                               ac.Target.DoppelspielMode != DoppelspielMode.Always &&
                                               ac.Target.DoppelspielMode != DoppelspielMode.None))
            .OrderBy(h => h.CreatedDate)
            .ToListAsync();

        // Step 2: Filter in memory with StationOptions check
        List<ActionGroup> pickOrders = pickCandidates
            .Where(a => a.Actions.Any(ac => ac.ActionType == ActionType.Pick &&
                                            ((ac.Target.Node != null && ac.Target.Node.Id == node.Id) ||
                                             ac.Target.StationOptions.Contains(node.Name)) &&
                                            ac is { IsRegisteredForDoppelspiel: true, DoppelspielPartner: null }))
            .ToList();

        // Step 3: Fetch drop candidates from database (without StationOptions check)
        List<ActionGroup> dropCandidates = await context.Set<ActionGroup>()
            .Where(a => a.CanRunDoppelspiel &&
                        a.CorrelationId != orderToIgnore &&
                        a.CompletedDate == null &&
                        a.Actions.Any(ac => ac.ActionType == ActionType.Drop &&
                                            ac.DoppelspielPartner == null))
            .OrderBy(h => h.ConsideredForDoppelspielDate)
            .ToListAsync();

        // Step 4: Filter in memory and count
        int drops = dropCandidates
            .Count(a => a.Actions.Any(ac => ac.ActionType == ActionType.Drop &&
                                            ((ac.Target.Node != null && ac.Target.Node.Id == node.Id) ||
                                             ac.Target.StationOptions.Contains(node.Name)) &&
                                            ac.DoppelspielPartner == null));

        if (pickOrders.Count <= drops)
        {
            // All picks will be part of a Doppelspiel
            return null;
        }

        return pickOrders.Skip(drops).FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<bool> HasActionGroupsForStationGroup(ICollection<Guid> stations, Guid? agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<ActionGroup>()
            .Where(a => a.IsQueued
                        && a.CompletedDate == null
                        && (a.AssignedAgv == null || a.AssignedAgv != agvCorrelationId)
                        && (a.Actions.Any(ac => ac.ActionType == ActionType.Drop && ac.Target.Node != null && stations.Contains(ac.Target.Node.Id) && ac.CompletedDate == null) ||
                            a.Actions.Any(ac => ac.ActionType == ActionType.Pick && ac.Target.Node != null && stations.Contains(ac.Target.Node.Id) && ac.CompletedDate == null)))
            .AnyAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> IsActionGroupQueued(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var actionGroup = await context.Set<ActionGroup>().SingleOrDefaultAsync(a => a.CorrelationId == correlationId);
        return actionGroup?.IsQueued ?? false;
    }

    private async Task<IReadOnlyList<ActionGroup>> GetAgvActionGroupsInState(Guid agvCorrelationId, List<ActionGroupState> actionGroupStates)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Actions are auto-included.
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node);

        return await actionGroups.Where(ag =>
                ag.AssignedAgv == agvCorrelationId
                && actionGroupStates.Contains(ag.State))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ActionGroup>> GetCurrentActionGroupsForStation(string stationName)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Set<ActionGroup>()
            .AsNoTracking()
            .Include(a => a.Actions)
            .ThenInclude(a => a.Target)
            .ThenInclude(t => t.Node)
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .Where(a =>
                    a.IsQueued &&
                    a.Actions.Any(action => action.Target.Node!.Name == stationName &&
                                            (action.State == ActionState.Created || action.State == ActionState.Started || action.State == ActionState.Paused)) &&
                    (a.State == ActionGroupState.Started || // currently running orders to this station
                     a.State == ActionGroupState.Paused || // or paused orders with not completed action to this station
                     a.Actions.Any(action => action.ActionType == ActionType.Charge) || (a.Actions.Count == 1 && a.Actions.First().ActionType == ActionType.Move)) // park and charge actions
            ).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ActionGroup>> GetPausedActionGroupsForStation(string stationName)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<ActionGroup>()
            .AsNoTracking()
            .Include(a => a.Actions)
            .ThenInclude(a => a.Target)
            .ThenInclude(t => t.Node)
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .Where(a =>
                a.Actions.Any(action => action.Target.Node!.Name == stationName) &&
                a.State == ActionGroupState.Paused
            ).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<ActionGroup>> GetNotStartedActionGroupsToTargetsForAgv(Guid agvId, ICollection<Guid> targets)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>().AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node);
        return await actionGroups.Where(ag => ag.AssignedAgv == agvId
                                              && ag.State == ActionGroupState.Created
                                              && ag.Actions.All(a => targets.Contains(a.Target.NodeId!.Value)))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ActionGroup?> GetNextPickAtNodeAsync(Guid nodeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node);
        return await actionGroups.Where(ag =>
                ag.StartedDate == null && ag.State == ActionGroupState.Created && (ag.EstimatedDuration == null || ag.EstimatedDuration == TimeSpan.Zero) && ag.IsQueued && !ag.IsManual &&
                ag.Actions.Any(a => a.ActionType == ActionType.Pick && a.Target.NodeId!.Value == nodeId && a.StartedDate == null))
            .OrderBy(ac => ac.CreatedDate)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<AgvActionGroupsByState> GetLatestActionGroupsByStateForAgvAsync(Guid agvCorrelationId)
    {
        var states = new List<ActionGroupState> { ActionGroupState.Created, ActionGroupState.Started, ActionGroupState.Completed };
        IReadOnlyList<ActionGroup> actionGroups = await GetActionGroupsAsync(states, assignedAgvCorrelationId: agvCorrelationId, maxItems: 3);

        var created = actionGroups.FirstOrDefault(ag => ag.State == ActionGroupState.Created);
        var started = actionGroups.FirstOrDefault(ag => ag.State == ActionGroupState.Started);
        var completed = actionGroups.FirstOrDefault(ag => ag.State == ActionGroupState.Completed);

        return new AgvActionGroupsByState(created, started, completed);
    }
    
    /// <inheritdoc />
    public async Task<ICollection<Guid>> GetAgvsMovingCargo(ICollection<Guid> assignedAgvs)
    {
        if (assignedAgvs.Count == 0)
        {
            return new List<Guid>();
        }
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<Guid> filteredAgvIds = await context.Set<ActionGroup>()
            .Include(g => g.Actions)
            .ThenInclude(a => a.AgvActionNodes)
            .ThenInclude(a => a.Node)
            .Where(ag => (ag.State == ActionGroupState.Created || ag.State == ActionGroupState.Started)
                         && assignedAgvs.Contains(ag.AssignedAgv!.Value)
                         && ag.Actions.Any(a => a.ActionType == ActionType.Pick || a.ActionType == ActionType.Drop))
            .Select(ag => ag.AssignedAgv!.Value).Distinct().ToListAsync();
        
        return filteredAgvIds;
    }

    /// <inheritdoc />
    public async Task DeleteOutdatedActionGroupsAsync(DateTime outDatedTime)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(30)); // the default command timeout is 30s, which could be too short, depending on the amount of data

        var outdated = new SqlParameter("OutdatedTime", outDatedTime);

        await dbContext.Database.ExecuteSqlRawAsync("EXECUTE [dbo].[HousekeepActionGroups] @OutdatedTime", outdated);
    }
}