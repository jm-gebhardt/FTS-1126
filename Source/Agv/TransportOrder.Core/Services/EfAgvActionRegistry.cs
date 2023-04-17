using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;

/// <summary>
/// Repository to access order actions
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class EfAgvActionRegistry : IAgvActionRegistry
{
    private readonly IDbContextFactory<SagaPersistenceDbContext> _dbContextFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EfAgvActionRegistry(IDbContextFactory<SagaPersistenceDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public void Insert(AgvAction agvAction)
    {
        using var context = _dbContextFactory.CreateDbContext();
        context.Add(agvAction);
        context.SaveChanges();
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public AgvAction? GetActionById(Guid id)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return context.Set<AgvAction>().Find(id);
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
    public AgvAction? GetSubsequentAction(Guid parentId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        return context.Set<AgvAction>()
            .Where(a => a.ParentId == parentId && a.State == ActionState.Assigned)
            .OrderBy(a => a.Sequence)
            .FirstOrDefault();
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public AgvAction? GetSubsequentAction(string agvId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        return context.Set<AgvAction>()
            .Where(a => a.AgvId == agvId && a.State == ActionState.Assigned)
            .OrderBy(a => a.Sequence)
            .FirstOrDefault();
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
            .GroupBy(a => a.OrderId)
            .Where(g => g.All(action => action.State != ActionState.Started && action.State != ActionState.Completed))
            .Select(g => new ActionGroup(
                g.Key.ToGuid(),
                g.Select(x => x).OrderBy(x => x.Sequence).ToList())
            ).ToList();

        return notStartedActionGroups;
    }

    /// <inheritdoc cref="IAgvActionRegistry"/>
    public List<AgvAction> GetUnfinishedActions(string agvId)
    {
        using var context = _dbContextFactory.CreateDbContext();

        List<AgvAction> unfinishedActions = context.Set<AgvAction>()
            .Where(a => a.AgvId == agvId && a.State == ActionState.Started).ToList();

        return unfinishedActions;
    }
}