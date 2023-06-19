using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;

/// <summary>
/// Repository to access ActionGroups
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class EfActionGroupRegistry : IActionGroupRegistry
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EfActionGroupRegistry(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public async Task InsertAsync(ActionGroup actionGroup)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(actionGroup);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(IList<ActionGroupState>? states = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .OrderByDescending(g => g.CreatedDate);

        if (states?.Any() ?? false)
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

        List<ActionGroup> actionGroupsAsync = await actionGroups
            .Include(g => g.Actions)
            .AsNoTracking()
            .Take(maxItems)
            .ToListAsync();
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
    public async Task<IReadOnlyDictionary<Guid, ActionGroup?>> GetCurrentAssignedActionGroupsAsync(IEnumerable<Guid> agvCorrelationIds)
    {
        // Once Agv is assigned to ActionGroup, Status is immediately set to started (and StartedDate), therefore we can use StartedDate to get current assigned ActionGroup  
        List<Task<(Guid agvCorrelationId, ActionGroup? actionGroup)>> tasks = agvCorrelationIds
            .Select(async agvCorrelationId =>
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync();
                DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

                var actionGroup = await queryable.OrderByDescending(g => g.StartedDate)
                    .Include(g => g.Actions)
                    .FirstOrDefaultAsync(a => a.AssignedAgv == agvCorrelationId);

                return (agvCorrelationId, actionGroup);
            }).ToList();
        await Task.WhenAll(tasks);

        Dictionary<Guid, ActionGroup?> actions = tasks.ToDictionary(t => t.Result.agvCorrelationId, t => t.Result.actionGroup);

        return actions;
    }

    /// <inheritdoc />
    public async Task<ActionGroup?> GetActionGroupAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable
            .FindAsync(correlationId);

        if (actionGroup != null)
        {
            await context.Entry(actionGroup)
                .Collection(g => g.Actions)
                .LoadAsync();
        }

        return actionGroup;
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

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public async Task<AgvAction?> GetActionByIdAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

        var action = await queryable
            .Include(actionGroup => actionGroup.Actions)
            .AsNoTracking()
            .SingleOrDefaultAsync(actionGroup => actionGroup.Actions.Any(a => a.CorrelationId == correlationId));

        return action?.Actions.SingleOrDefault(a => a.CorrelationId == correlationId);
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public async Task<AgvAction> GetRequiredActionByIdAsync(Guid correlationId)
    {
        var action = await GetActionByIdAsync(correlationId);
        if (action != null)
        {
            return action;
        }

        throw new ActionNotFoundException("ActionCorrelationId", nameof(correlationId), correlationId);
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public async Task DeleteByParentIdAsync(Guid parentId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        queryable.RemoveRange(queryable.Where(a => a.CorrelationId == parentId));
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<ActionGroupState> UpdateActionAndGroupStateAsync(AgvAction action, IAgv agv, ActionState state)
    {
        var actionGroupCorrelationId = action.ParentId;

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable.FindAsync(actionGroupCorrelationId);

        if (actionGroup == null)
        {
            throw new ActionGroupNotFoundException(actionGroupCorrelationId);
        }
        await context.Entry(actionGroup).Collection(g => g.Actions).LoadAsync();

        action.UpdateActionState(state);

        var actionToUpdate = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId);
        context.Entry(actionToUpdate).CurrentValues.SetValues(action);

        actionGroup.Assign(agv);
        actionGroup.UpdateActionGroupState();

        await context.SaveChangesAsync();

        return actionGroup.State;
    }

    /// <inheritdoc/>
    public async Task<bool> HasUnfinishedDropActionsAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        bool hasUnfinishedDropActions = await queryable.Where(actionGroup =>
                actionGroup.AssignedAgv == agvCorrelationId &&
                actionGroup.State != ActionGroupState.Completed &&
                actionGroup.Actions.Any(a => a.State != ActionState.Completed && a.ActionType == ActionType.Drop)
            )
            .AsNoTracking()
            .AnyAsync();

        return hasUnfinishedDropActions;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ActionGroup>> GetNotStartedActionGroupsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        List<ActionGroup> notStartedActionGroups = await queryable
            .Where(actionGroup => actionGroup.State == ActionGroupState.Created)
            .Include(g => g.Actions)
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
                                  actionGroup.State == ActionGroupState.Started)
            .Include(g => g.Actions)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        var subsequentAction = startedGroup?.Actions.Where(action => action.State == ActionState.Created).MinBy(a => a.Sequence);

        if (subsequentAction == null)
        {
            throw new ActionNotFoundException(nameof(subsequentAction), nameof(agvCorrelationId), agvCorrelationId);
        }
        return subsequentAction;
    }
}