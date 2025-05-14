using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="IHostOrderRepository"/>>.
public class EfHostOrderRepository(PrismatCommunicationDbContext context) : EfRepository<HostOrder, PrismatCommunicationDbContext>(context), IHostOrderRepository
{
    private readonly PrismatCommunicationDbContext _context = context;

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task UpdateAsync(HostOrder hostOrder)
    {
        _context.Set<HostOrder>().Update(hostOrder);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<ICollection<HostOrder>> GetLatestUnfinishedOrdersByHu(string hu, DateTime creationDate)
    {
        return await _context.Set<HostOrder>().Where(o => o.FinishedDate == null && o.CanceledDate == null && o.Hu == hu && o.CreatedDate < creationDate).ToListAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetLatestUnfinishedOrderByHu(string hu)
    {
        return await _context.Set<HostOrder>().Where(o => o.FinishedDate == null && o.CanceledDate == null && o.Hu == hu).OrderByDescending(o => o.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<Guid?> GetNextDropOrderHeadingTowardsDoppelspielTriggerPoint(string dropTarget)
    {
        var order = await _context.Set<HostOrder>().Where(
                h => !h.PassedTriggerPoint
                     && h.CanceledDate == null
                     && h.FinishedDate == null
                     && h.StartedDate != null
                     && h.IsQueued
                     && h.DoppelspielPartner == null
                     && h.Actions.Any(a => a.ActionType == ActionType.Drop && a.Node == dropTarget)
                     && h.Actions.Any(a  => a.ActionType == ActionType.Pick && a.FinishedDate != null))
            .OrderBy(o => o.StartedDate).FirstOrDefaultAsync();
        return order?.CorrelationId;
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetNextHostOrderForStationGroup(ICollection<string> sources, ICollection<string> targets)
    {
        return await _context.Set<HostOrder>().Where(
                h => !h.IsQueued &&
                     h.CanceledDate == null &&
                     h.FinishedDate == null &&
                     h.StartedDate == null &&
                     h.FailedDate == null &&
                     (h.Actions.Any(a => a.ActionType == ActionType.Drop && targets.Contains(a.Node)) ||
                      h.Actions.Any(a => a.ActionType == ActionType.Pick && sources.Contains(a.Node))))
            .OrderBy(h => h.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<bool> HasCurrentHostOrdersForStationGroup(ICollection<string> sources, ICollection<string> targets)
    {
       return await _context.Set<HostOrder>().Where(
            h => h.IsQueued &&
                 h.CanceledDate == null &&
                 h.FinishedDate == null &&
                 h.StartedDate != null &&
                 h.FailedDate == null &&
                 (h.Actions.Any(a => a.ActionType == ActionType.Drop && targets.Contains(a.Node) && a.FinishedDate == null) ||
                  h.Actions.Any(a => a.ActionType == ActionType.Pick && sources.Contains(a.Node) && a.FinishedDate == null)))
            .AnyAsync();
    }
}