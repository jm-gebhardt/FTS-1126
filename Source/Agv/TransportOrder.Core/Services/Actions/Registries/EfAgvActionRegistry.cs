using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;

/// <summary>
/// Repository to access order actions
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class EfAgvActionRegistry : IAgvActionRegistry
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EfAgvActionRegistry(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public bool Insert(ActionGroup actionGroup)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IQueryable<ActionGroup> GetActionGroups(ActionGroupState[] states, DateTime? fromDate = null, DateTime? toDate = null, int limit = 0)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ActionGroup? GetActionGroup(Guid correlationId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ActionGroup GetRequiredActionGroup(Guid correlationId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public void Insert(AgvAction agvAction)
    {
        using var context = _dbContextFactory.CreateDbContext();
        context.Add(agvAction);
        context.SaveChanges();
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public AgvAction? GetActionById(Guid correlationId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return context.Set<AgvAction>().Find(correlationId);
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public AgvAction GetRequiredActionById(Guid correlationId)
    {
        var action = GetActionById(correlationId);
        if (action != null)
        {
            return action;
        }
        
        throw new ActionNotFoundException("ActionCorrelationId", nameof(correlationId), correlationId);
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
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

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public List<AgvAction> GetActionsByParentId(Guid parentId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        return context.Set<AgvAction>()
            .Where(a => a.ParentId == parentId)
            .ToList();
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
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

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public AgvAction? GetSubsequentActionByParentId(Guid parentId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        return context.Set<AgvAction>()
            .Where(a => a.ParentId == parentId && a.State == ActionState.Assigned)
            .OrderBy(a => a.Sequence)
            .FirstOrDefault();
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public async Task<AgvAction> GetSubsequentActionAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // this implementation materializes the whole table
        IEnumerable<IGrouping<Guid, AgvAction>> actionGroups = (await context.Set<AgvAction>()
            .ToListAsync())
            .GroupBy(a => a.ParentId);

        IGrouping<Guid, AgvAction>? actionsFromGroup = actionGroups.SingleOrDefault(actionGroup =>
            actionGroup.Any(action => action.AssignedAgv == agvCorrelationId) &&
            actionGroup.Any(action => action.State == ActionState.Completed) &&
            actionGroup.Any(action => action is { State: ActionState.Created, ActionType: ActionType.Drop }));

        var subsequentAction = actionsFromGroup?.Where(action => action.State == ActionState.Created).MinBy(a => a.Sequence);

        if (subsequentAction == null)
        {
            throw new ActionNotFoundException(nameof(subsequentAction), nameof(agvCorrelationId), agvCorrelationId);
        }
        return subsequentAction;
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
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

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public List<ActionGroup> GetNotStartedActionGroups()
    {
        using var context = _dbContextFactory.CreateDbContext();

        List<ActionGroup> notStartedActionGroups = context.Set<AgvAction>()
            .GroupBy(a => a.ParentId)
            .Where(g => g.All(action => action.State != ActionState.Started && action.State != ActionState.Completed))
            .Select(g => new ActionGroup(
                g.Key,
                g.Select(x => x).OrderBy(x => x.Sequence).ToList())
            ).ToList();

        return notStartedActionGroups;
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public bool HasUnfinishedDropActions(Guid agvCorrelationId)
    {
        using var context = _dbContextFactory.CreateDbContext();
        
        IQueryable<IGrouping<Guid, AgvAction>> unfinishedActions = context.Set<AgvAction>()
            .GroupBy(a => a.ParentId)
            .Where(actionGroup => actionGroup.Any(action => action.AssignedAgv == agvCorrelationId) &&
                                  actionGroup.Any(action => action.State != ActionState.Completed &&
                                                            action.ActionType == ActionType.Drop));

        bool hasUnfinishedDropActions = unfinishedActions.Any();
        return hasUnfinishedDropActions;
    }

    /// <inheritdoc />
    public async Task SetActionStartedAsync(AgvAction action)
    {
        action.State = ActionState.Started;
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var actionInRepository = await context.Set<AgvAction>().FindAsync(action.CorrelationId);

        if (actionInRepository == null)
        {
            await context.AddAsync(action);
        }
        else
        {
            // overwrite whole entity
            context.Entry(actionInRepository).CurrentValues.SetValues(action);
        }

        await context.SaveChangesAsync();
    }
}