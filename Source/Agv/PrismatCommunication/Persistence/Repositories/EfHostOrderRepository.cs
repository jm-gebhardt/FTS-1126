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
                 && h.Actions.All(a => configuration.PrismatIncompleteDropTargets.Contains(a.Node)))
            .AnyAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task<bool> HasDropOrdersHeadingTowardsCargoTriggerPoint()
    {
        return await _context.Set<HostOrder>().Where(
            h => !h.PassedTriggerPoint
                 && h.StartedDate != null
                 && h.Actions.All(a => configuration.PrismatIncompletePickTargets.Contains(a.Node)))
            .AnyAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async  Task<HostOrder?> GetNextIncompleteOrderWithUnknownPickTarget()
    {
        return await _context.Set<HostOrder>().Where(
                h => !h.PassedTriggerPoint
                     && h.StartedDate == null
                     && h.AssignedAgv == null
                     && h.Actions.All(a => configuration.PrismatIncompletePickTargets.Contains(a.Node)))
            .OrderBy(h => h.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task<HostOrder?> GetNextIncompleteOrderWithUnknownDropTarget()
    {
        return await _context.Set<HostOrder>().Where(
                h => !h.PassedTriggerPoint
                     && h.AssignedAgv == null
                     && h.StartedDate == null
                     && h.Actions.All(a => configuration.PrismatIncompleteDropTargets.Contains(a.Node)))
            .OrderBy(h => h.CreatedDate).FirstOrDefaultAsync();
    }
}