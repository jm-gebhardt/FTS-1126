using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Extensions;
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
    public async Task<IReadOnlyList<ActionGroup>> GetActionGroupsAsync(IEnumerable<ActionGroupState> states, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 0)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();

        IQueryable<ActionGroup> actionGroups = queryable
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

        List<ActionGroup> actionGroupsAsync = await actionGroups.Take(maxItems).ToListAsync();
        return actionGroupsAsync;
    }

    /// <inheritdoc />
    public async Task<ActionGroup?> GetActionGroupAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable.FindAsync(correlationId);
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

        var action = await queryable.SingleOrDefaultAsync(actionGroup => actionGroup.Actions.Any(a => a.CorrelationId == correlationId));

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
        queryable
            .RemoveRange(
                queryable
                    .Where(a => a.CorrelationId == parentId)
            );
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateActionAndGroupStateAsync(AgvAction action, IAgv agv, ActionState state)
    {
        var actionGroupCorrelationId = action.ParentId;

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var actionGroup = await queryable.FindAsync(actionGroupCorrelationId);

        if (actionGroup == null)
        {
            throw new ActionGroupNotFoundException(actionGroupCorrelationId);
        }

        var actionToUpdate = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId);
        actionToUpdate.State = state;

        actionGroup.AssignAgvToActionGroup(agv);
        actionGroup.UpdateActionGroupState();

        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> HasUnfinishedDropActionsAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        List<ActionGroup> unfinishedGroups = await queryable.Where(actionGroup =>
            actionGroup.AssignedAgv == agvCorrelationId &&
            actionGroup.State != ActionGroupState.Completed).ToListAsync();

        // must materialize here, if Any statement is put in the IQueryable it can not be translated
        bool hasUnfinishedDropActions = unfinishedGroups.Any(g => g.Actions.Any(a => a.State != ActionState.Completed && a.ActionType == ActionType.Drop));

        return hasUnfinishedDropActions;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ActionGroup>> GetNotStartedActionGroupsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        List<ActionGroup> notStartedActionGroups = await queryable.Where(actionGroup => actionGroup.State != ActionGroupState.Created).ToListAsync();

        return notStartedActionGroups;
    }

    /// <inheritdoc/>
    public async Task<AgvAction> GetSubsequentActionAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<ActionGroup> queryable = context.Set<ActionGroup>();
        var startedGroup = await queryable
            .Where(actionGroup => actionGroup.AssignedAgv == agvCorrelationId &&
                                  actionGroup.State == ActionGroupState.Started).FirstOrDefaultAsync();

        var subsequentAction = startedGroup?.Actions.Where(action => action.State == ActionState.Created).MinBy(a => a.Sequence);

        if (subsequentAction == null)
        {
            throw new ActionNotFoundException(nameof(subsequentAction), nameof(agvCorrelationId), agvCorrelationId);
        }
        return subsequentAction;
    }
}