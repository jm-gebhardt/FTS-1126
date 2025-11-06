using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using JetBrains.Annotations;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Repository to access ActionGroups
/// </summary>
[PublicAPI]
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

    /// <inheritdoc />
    public async Task<bool> AddAsync(ActionGroup actionGroup)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(actionGroup);
        return await context.SaveChangesAsync() > 0;
    }
    
    /// <inheritdoc />
    public async Task<bool> AddOrUpdateAsync(ActionGroup actionGroup)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        if (await context.Set<ActionGroup>().AnyAsync(a => a.CorrelationId == actionGroup.CorrelationId))
        {
            await UpdateActionGroupAndActions(actionGroup, context);
            return true;
        }
        await context.AddAsync(actionGroup);
        await context.SaveChangesAsync();
        return false;
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
        await context.SaveChangesAsync();
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

        actionGroups = actionGroups.OrderByProperty(nameof(SortBy.CreatedDate), isDescending: false).ThenByDescending(g => g.HasPriority);
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
            actionGroups = actionGroups.Where(a => a.Actions.Any(action => action.Target.Node!.Id == sourceNodeId));
        }

        if (sourceNodeId == null && targetNodeId != null)
        {
            actionGroups = actionGroups.Where(a => a.Actions.Any(action => action.Target.Node!.Id == targetNodeId));
        }

        if (sourceNodeId != null && targetNodeId != null)
        {
            actionGroups = actionGroups.Where(a =>
                a.Actions.Any(action => action.Target.Node!.Id == sourceNodeId) &&
                a.Actions.Any(action => action.Target.Node!.Id == targetNodeId));
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
            .OrderByProperty(sortBy.ToString(), isDescending: sortDescending)
            .ThenByDescending(g => g.HasPriority)
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
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

        var actionGroup = await queryable.OrderByDescending(g => g.StartedDate)
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
        var actionGroup = await queryable.AsNoTracking()
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
        var actionGroup = await context.Set<ActionGroup>()
            .SingleOrDefaultAsync(a => a.Actions.Any(aa => aa.CorrelationId == correlationId));
        return actionGroup ?? throw new ActionGroupNotFoundException(correlationId);
    }

    /// <inheritdoc />
    public async Task<ICollection<ActionGroup>> GetRunningIdleActionGroups()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return (await context.Set<ActionGroup>()
                .Where(a => a.State == ActionGroupState.Started && !a.IsManual && !a.HasPriority).ToListAsync())
            .Where(a => !a.IsMovingCargo).ToList();
    }

    /// <inheritdoc />
    public async Task<ICollection<ActionGroup>> GetRunningChargeActionGroups()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<ActionGroup>()
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
            actionGroup.Assign(agv);
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
            .AnyWithNoLockAsync();

        return hasUnfinishedDropActions;
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
            .OrderBy(g => g.CreatedDate)
            .ThenByDescending(g => g.IsManual)
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
    public async Task<bool> AgvHasPriorityActionGroups(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        DbSet<ActionGroup> actionGroups = context.Set<ActionGroup>();

        return await actionGroups.AnyAsync(a =>
            a.AssignedAgv == agvCorrelationId && a.State == ActionGroupState.Created && a.HasPriority && a.IsQueued);
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
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Set<ActionGroup>().Update(actionGroup);
        int result = await context.SaveChangesAsync();
        _logger.LogInformation("Update action group async save affected {RowsAffected} rows", result);
        await context.Entry(actionGroup).ReloadAsync();
        _logger.LogInformation("Action group {ActionGroupCorrelationId} state after saving update {State}", actionGroup.CorrelationId, actionGroup.State);
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

            return actionGroups.Where(ag =>
                    ag.StartedDate.HasValue && ag.CompletedDate.HasValue && ag.EstimatedDuration.HasValue &&
                    ag.AssignedAgv == agvCorrelationId &&
                    ag.Actions.All(a => targetNodes.Contains(a.Target.Node!.Id)))
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
    public async Task<ActionGroup?> GetCreatedChargingActionGroupForAgvAsync(Guid agvCorrelationId)
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
            .OrderByDescending(ag => ag.CreatedDate)
            .FirstOrDefaultAsync();
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
    public async Task<AgvAction?> GetLastCompletedActionAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable
            .Where(ag => ag.AssignedAgv == agvCorrelationId)
            .OrderByDescending(ag => ag.StartedDate)
            .ThenByDescending(ag => ag.CompletedDate)
            .Include(ag => ag.Actions)
            .AsNoTracking()
            .FirstOrDefaultAsync(ag => ag.Actions.Any(a => a.State == ActionState.Completed));

        var action = actionGroup?.Actions.OrderByDescending(a => a.CompletedDate).FirstOrDefault(a => a.CompletedDate != null);
        return action;
    }

    /// <inheritdoc/>
    public async Task<AgvAction?> GetLastAbortedPickDropActionAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable
            .Include(ag => ag.Actions)
            .AsNoTracking()
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
            .AsNoTracking();

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

        return (await actionGroups.Where(a => a.HasPriority && a.IsQueued &&
                                              a.AssignedAgv != null && agvs.Contains(a.AssignedAgv!.Value) &&
                                              (a.State == ActionGroupState.Started || a.State == ActionGroupState.Created))
            .ToListAsync()).Select(a => a.AssignedAgv!.Value).ToList();
    }

    /// <inheritdoc/>
    public async Task<ICollection<ActionGroup>> GetCurrentActionGroupsToTarget(Guid targetId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking();
        return await actionGroups.Where(ag =>
                (ag.State == ActionGroupState.Started || ag.State == ActionGroupState.Created) &&
                ag.Actions.Any(a => (a.State == ActionState.Started || a.State == ActionState.Created) && a.Target.Node!.Id == targetId))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<ActionGroup>> GetNextPicksAtTarget(Guid targetId, ICollection<Guid> assignedAgvs)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking();
        return await actionGroups.Where(ag =>
                !ag.IsQueued &&
                ag.State == ActionGroupState.Created &&
                ag.AssignedAgv != null &&
                assignedAgvs.Contains(ag.AssignedAgv.Value) &&
                ag.Actions.Any(a => a.ActionType == ActionType.Pick && a.Target.Node!.Id == targetId))
            .ToListAsync();
    }

    private async Task<IReadOnlyList<ActionGroup>> GetAgvActionGroupsInState(Guid agvCorrelationId, List<ActionGroupState> actionGroupStates)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Actions are auto-included.
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking();

        return await actionGroups.Where(ag =>
                ag.AssignedAgv == agvCorrelationId
                && actionGroupStates.Contains(ag.State))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ActionGroup>> GetCurrentActionGroupsForStation(string stationName)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Set<ActionGroup>().AsNoTracking().Where(a =>
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
        return await context.Set<ActionGroup>().AsNoTracking().Where(a =>
            a.Actions.Any(action => action.Target.Node!.Name == stationName) &&
            a.State == ActionGroupState.Paused
        ).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<ActionGroup>> GetNotStartedActionGroupsToTargetsForAgv(Guid agvId, ICollection<Guid> targets)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>().AsNoTracking();
        return await actionGroups.Where(ag => ag.AssignedAgv == agvId
                                              && ag.State == ActionGroupState.Created
                                              && ag.Actions.All(a => targets.Contains(a.Target.Node != null ? a.Target.Node.Id : Guid.Empty)))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ActionGroup?> GetNextPickAtNodeAsync(Guid nodeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .AsNoTracking();
        return await actionGroups.Where(ag =>
                ag.StartedDate == null && ag.State == ActionGroupState.Created && (ag.EstimatedDuration == null || ag.EstimatedDuration == TimeSpan.Zero) && ag.IsQueued && !ag.IsManual &&
                ag.Actions.Any(a => a.ActionType == ActionType.Pick && a.Target.Node!.Id == nodeId && a.StartedDate == null))
            .OrderBy(ac => ac.CreatedDate)
            .FirstOrDefaultAsync();
    }
}