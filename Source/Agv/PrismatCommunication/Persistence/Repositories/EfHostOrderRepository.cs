using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="IHostOrderRepository"/>>.
public class EfHostOrderRepository(PrismatCommunicationDbContext context, KaeserGraphConfiguration configuration) : EfRepository<HostOrder, PrismatCommunicationDbContext>(context), IHostOrderRepository
{
    private readonly PrismatCommunicationDbContext _context = context;

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task UpdateAsync(HostOrder hostOrder)
    {
        _context.Set<HostOrder>().Update(hostOrder);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task<bool> HasDropOrdersHeadingTowardsDecisionPoint()
    {
        return await _context.Set<HostOrder>().Where(
            h => !h.PassedTriggerPoint
                 && h.StartedDate != null
                 && h.CanceledDate == null
                 && h.Actions.All(a => configuration.PrismatIncompleteDropTargets.Contains(a.Node)))
            .AnyAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task<bool> HasDropOrdersHeadingTowardsCargoTriggerPoint()
    {
        return await _context.Set<HostOrder>().Where(
            h => !h.PassedTriggerPoint
                 && h.CanceledDate == null
                 && h.StartedDate != null
                 && h.Actions.All(a => configuration.PrismatIncompletePickTargets.Contains(a.Node)))
            .AnyAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task<bool> HasDropOrdersHeadingTowardsShortLoopTriggerPoint()
    {
        return await _context.Set<HostOrder>().Where(
                h => !h.PassedTriggerPoint
                     && h.CanceledDate == null
                     && h.StartedDate != null
                     && h.Actions.All(a => configuration.PrismatKnownTargets.Contains(a.Node))
                     && h.Actions.All(a => a.Node != configuration.PrismatAklPickTarget))
            .AnyAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async  Task<HostOrder?> GetNextIncompleteOrderWithUnknownPickTarget()
    {
        return await _context.Set<HostOrder>().Where(
                h => !h.PassedTriggerPoint
                     && h.CanceledDate == null
                     && h.StartedDate == null
                     && h.AssignedAgv == null
                     && !h.IsQueued
                     && h.Actions.All(a => configuration.PrismatIncompletePickTargets.Contains(a.Node)))
            .OrderBy(h => h.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task<HostOrder?> GetNextIncompleteOrderWithUnknownDropTarget()
    {
        return await _context.Set<HostOrder>().Where(
                h => !h.PassedTriggerPoint
                     && h.CanceledDate == null
                     && h.AssignedAgv == null
                     && !h.IsQueued
                     && h.StartedDate == null
                     && h.Actions.All(a => configuration.PrismatIncompleteDropTargets.Contains(a.Node)))
            .OrderBy(h => h.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task<HostOrder?> GetNextPickOrderForShortLoopDualOperation()
    {
        return await _context.Set<HostOrder>().Where(
                h =>  h.AssignedAgv == null
                      && h.CanceledDate == null
                      && h.StartedDate == null
                      && h.Actions.Any(a => a.Node == configuration.PrismatAklPickTarget && a.ActionType == ActionType.Pick))
            .OrderBy(h => h.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetByHu(string hu)
    {
        return await _context.Set<HostOrder>().FirstOrDefaultAsync(o => o.Hu == hu);
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetNextNotQueuedHostOrderPickingAt(string? pickTarget, bool unknownDrop = false)
    {
        return await _context.Set<HostOrder>().Where(
            h => h.CanceledDate == null 
                 && h.StartedDate == null
                 && h.AssignedAgv == null
                 && !h.IsQueued
                 && h.Actions.Any(a => 
                     (pickTarget != null && a.ActionType == ActionType.Pick && pickTarget == a.Node)
                     || (unknownDrop && a.ActionType == ActionType.Drop && configuration.PrismatIncompleteDropTargets.Contains(a.Node))
                     || (pickTarget == null && a.ActionType == ActionType.Pick && configuration.PrismatIncompletePickTargets.Contains(a.Node))
                 )
        ).OrderBy(o => o.CreatedDate)
            .FirstOrDefaultAsync();
    }
}