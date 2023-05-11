using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
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
    }

    /// <inheritdoc />
    public IQueryable<ActionGroup> GetActionGroups(IEnumerable<ActionGroupState> states, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0)
    {
        using var context = _dbContextFactory.CreateDbContext();
        IQueryable<ActionGroup> actionGroups = context.Set<ActionGroup>()
            .Where(g => states.Contains(g.State))
            .OrderBy(g => g.CreatedDate);

        if (fromDate != null)
        {
            actionGroups = actionGroups.Where(g => g.CreatedDate >= fromDate);
        }

        if (toDate != null)
        {
            actionGroups = actionGroups.Where(g => g.CreatedDate <= toDate);
        }

        return actionGroups.Take(maxItems);
    }

    /// <inheritdoc />
    public ActionGroup? GetActionGroup(Guid correlationId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return context.Set<ActionGroup>().Find(correlationId);
    }

    /// <inheritdoc />
    public ActionGroup GetRequiredActionGroup(Guid correlationId)
    {
        var actionGroup = GetActionGroup(correlationId);
        if (actionGroup == null)
        {
            throw new ActionGroupNotFoundException(correlationId);
        }

        return actionGroup;
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public AgvAction? GetActionById(Guid correlationId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return context.Set<AgvAction>().Find(correlationId);
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public AgvAction GetRequiredActionById(Guid correlationId)
    {
        var action = GetActionById(correlationId);
        if (action != null)
        {
            return action;
        }

        throw new ActionNotFoundException("ActionCorrelationId", nameof(correlationId), correlationId);
    }

    /// <inheritdoc/>
    public List<AgvAction> GetPendingActions(bool sortBySequenceAsc = false)
    {
        using var context = _dbContextFactory.CreateDbContext();

        IQueryable<AgvAction> agvActions = context.Set<AgvAction>()
            .Where(a => a.State == ActionState.Created || a.State == ActionState.Assigned);

        if (sortBySequenceAsc)
        {
            return agvActions
                .OrderBy(o => o.Sequence)
                .ToList();
        }

        return agvActions.ToList();
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public List<AgvAction> GetActionsByParentId(Guid parentId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        return context.Set<AgvAction>()
            .Where(a => a.ParentId == parentId)
            .ToList();
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public void UpdateState(Guid id, ActionState state)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var action = context.Set<AgvAction>().Find(id);
        if (action == null)
        {
            throw new ActionUpdateFailedException(id, nameof(state), state);
        }

        action.State = state;
        context.SaveChanges();
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public AgvAction? GetSubsequentActionByParentId(Guid parentId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        // return context.Set<AgvAction>()
        //     .Where(a => a.ParentId == parentId && a.State == ActionState.Assigned)
        //     .OrderBy(a => a.Sequence)
        //     .FirstOrDefault();

        return context.Set<ActionGroup>()
            .Where(g => g.CorrelationId == parentId && g.Actions.Any(a => a.State == ActionState.Assigned))
            .SelectMany(a => a.Actions).OrderBy(a => a.Sequence).FirstOrDefault();
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public async Task<AgvAction> GetSubsequentActionAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // this implementation materializes the whole table
        List<ActionGroup> actionGroups = (await context.Set<ActionGroup>().ToListAsync());

        // var actionsFromGroup = actionGroups.SingleOrDefault(actionGroup =>
        //     actionGroup.Any(action => action.AssignedAgv == agvCorrelationId) &&
        //     actionGroup.Any(action => action.State == ActionState.Completed) &&
        //     actionGroup.Any(action => action is { State: ActionState.Created, ActionType: ActionType.Drop }));
        var actionsFromGroup = actionGroups.SingleOrDefault(actionGroup => actionGroup.AssignedAgv == agvCorrelationId
                                                                           && actionGroup.Actions.Any(action => action.State == ActionState.Completed)
                                                                           && actionGroup.Actions.Any(action => action is { State: ActionState.Created, ActionType: ActionType.Drop }));

        var subsequentAction = actionsFromGroup?.Actions.Where(action => action.State == ActionState.Created).MinBy(a => a.Sequence);

        if (subsequentAction == null)
        {
            throw new ActionNotFoundException(nameof(subsequentAction), nameof(agvCorrelationId), agvCorrelationId);
        }
        return subsequentAction;
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public void DeleteByParentId(Guid parentId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        context.Set<AgvAction>()
            .RemoveRange(
                context.Set<AgvAction>()
                    .Where(a => a.ParentId == parentId)
            );
        context.SaveChanges();
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public List<ActionGroup> GetNotStartedActionGroups()
    {
        using var context = _dbContextFactory.CreateDbContext();

        // List<ActionGroup> notStartedActionGroups = context.Set<AgvAction>()
        //     .GroupBy(a => a.ParentId)
        //     .Where(g => g.All(action => action.State != ActionState.Started && action.State != ActionState.Completed))
        //     .Select(g => new ActionGroup(
        //         g.Key,
        //         g.Select(x => x).OrderBy(x => x.Sequence).ToList())
        //     ).ToList();

        List<ActionGroup> notStartedActionGroups = context.Set<ActionGroup>()
            .Where(g => g.Actions.All(action => action.State != ActionState.Started && action.State != ActionState.Completed)).ToList();

        return notStartedActionGroups;
    }

    /// <inheritdoc cref="IActionGroupRegistry"/>
    public bool HasUnfinishedDropActions(Guid agvCorrelationId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        // IQueryable<IGrouping<Guid, AgvAction>> unfinishedActions = context.Set<AgvAction>()
        //     .GroupBy(a => a.ParentId)
        //     .Where(actionGroup => actionGroup.Any(action => action.AssignedAgv == agvCorrelationId) &&
        //                           actionGroup.Any(action => action.State != ActionState.Completed &&
        //                                                     action.ActionType == ActionType.Drop));

        IQueryable<ActionGroup> unfinishedActions = context.Set<ActionGroup>()
            .Where(actionGroup => actionGroup.AssignedAgv == agvCorrelationId
                                  && actionGroup.Actions.Any(action => action.State != ActionState.Completed && action.ActionType == ActionType.Drop));

        bool hasUnfinishedDropActions = unfinishedActions.Any();
        return hasUnfinishedDropActions;
    }

    /// <inheritdoc />
    public async Task UpdateActionGroupAsync(ActionGroup actionGroup)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var actionGroupInRepository = await context.Set<ActionGroup>().FindAsync(actionGroup.CorrelationId);

        if (actionGroupInRepository == null)
        {
            throw new ActionGroupNotFoundException(actionGroup.CorrelationId);
        }

        // overwrite whole entity
        context.Entry(actionGroupInRepository).CurrentValues.SetValues(actionGroup);

        await context.SaveChangesAsync();
    }
}