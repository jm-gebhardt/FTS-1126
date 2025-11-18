using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="IHostOrderRepository"/>>.
public class EfHostOrderRepository(PrismatCommunicationDbContext context) : EfRepository<HostOrder, PrismatCommunicationDbContext>(context, true), IHostOrderRepository
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
    public async Task<Guid?> GetNextDropOrderEligibleForDoppelspiel(string[] dropTargets)
    {
        // Cast to IEnumerable to avoid C# 14 span overload resolution breaking change
        IEnumerable<string> targets = dropTargets;
        var order = await _context.Set<HostOrder>().Where(h => h.CanRunDoppelspiel
                                                               && h.CanceledDate == null
                                                               && h.FinishedDate == null
                                                               && h.IsQueued
                                                               && h.Actions.Any(a => a.ActionType == ActionType.Drop && targets.Contains(a.Node))
                                                               && !h.Actions.Any(a => a.ActionType == ActionType.Drop && a.IsRegisteredForDoppelspiel)
                                                               && h.Actions.Any(a => a.ActionType == ActionType.Pick && a.FinishedDate != null))
            .OrderBy(o => o.StartedDate)
            .ThenBy(o => o.ConsideredForDoppelspielDate).FirstOrDefaultAsync();
        return order?.CorrelationId;
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetNextHostOrderForStationGroup(ICollection<string> sources, ICollection<string> targets)
    {
        return await _context.Set<HostOrder>().Where(h => !h.IsQueued &&
                                                          h.CanceledDate == null &&
                                                          h.FinishedDate == null &&
                                                          h.StartedDate == null &&
                                                          h.FailedDate == null &&
                                                          (h.Actions.Any(a => a.ActionType == ActionType.Drop && targets.Contains(a.Node)) ||
                                                           h.Actions.Any(a => a.ActionType == ActionType.Pick && sources.Contains(a.Node))))
            .OrderBy(h => h.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<bool> HasHostOrdersForStationGroup(ICollection<string> sources, ICollection<string> targets, Guid? agvCorrelationId)
    {
        return await _context.Set<HostOrder>().Where(h =>
                h.IsQueued &&
                h.CanceledDate == null &&
                h.FinishedDate == null &&
                h.FailedDate == null &&
                (h.Actions.Any(a => a.ActionType == ActionType.Drop && targets.Contains(a.Node) && a.FinishedDate == null) ||
                 h.Actions.Any(a => a.ActionType == ActionType.Pick && sources.Contains(a.Node) && a.FinishedDate == null)) &&
                (h.AssignedAgv == null || h.AssignedAgv != agvCorrelationId)
            )
            .AnyAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetNextPickForSources(ICollection<string> sources, ICollection<Guid>? ordersToIgnore = null)
    {
        ordersToIgnore ??= [];

        return await _context.Set<HostOrder>().Where(h => !h.IsQueued &&
                                                          !ordersToIgnore.Contains(h.CorrelationId)&&
                                                          h.CanceledDate == null &&
                                                          h.FinishedDate == null &&
                                                          h.StartedDate == null &&
                                                          h.FailedDate == null &&
                                                          h.AssignedAgv == null &&
                                                          h.Actions.Any(a => a.ActionType == ActionType.Pick && sources.Contains(a.Node)))
            .OrderBy(h => h.CreatedDate)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetNextUnmatchedDoppelspielPick(ICollection<string> sources, ICollection<string> targets, Guid? correlationId)
    {
        List<HostOrder> pickOrders = await _context.Set<HostOrder>().Where(h => !h.IsQueued &&
                                                                                h.CanceledDate == null &&
                                                                                h.FinishedDate == null &&
                                                                                h.StartedDate == null &&
                                                                                h.FailedDate == null &&
                                                                                h.Actions.Any(a => a.ActionType == ActionType.Pick && sources.Contains(a.Node) && a.IsRegisteredForDoppelspiel && a.DoppelspielPartner == null))
            .OrderBy(h => h.CreatedDate).ToListAsync();

        int drops = await _context.Set<HostOrder>().Where(h =>
                h.CanRunDoppelspiel &&
                h.CorrelationId != correlationId &&
                h.CanceledDate == null &&
                h.FinishedDate == null &&
                h.FailedDate == null &&
                h.Actions.Any(a => a.ActionType == ActionType.Drop && targets.Contains(a.Node) && a.DoppelspielPartner == null))
            .OrderBy(h => h.ConsideredForDoppelspielDate).CountAsync();

        if (pickOrders.Count <= drops)
        {
            // All picks will be part of a Doppelspiel
            return null;
        }

        return pickOrders.Skip(drops).FirstOrDefault();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetNextUnmatchedDoppelspielDrop(ICollection<string> sources, ICollection<string> targets, Guid? correlationId)
    {
        int pickCount = await _context.Set<HostOrder>().Where(h => !h.IsQueued &&
                                                                   h.CorrelationId != correlationId &&
                                                                   h.CanceledDate == null &&
                                                                   h.FinishedDate == null &&
                                                                   h.StartedDate == null &&
                                                                   h.FailedDate == null &&
                                                                   h.Actions.Any(a => a.ActionType == ActionType.Pick && sources.Contains(a.Node) && a.IsRegisteredForDoppelspiel && a.DoppelspielPartner == null))
            .OrderBy(h => h.CreatedDate).CountAsync();

        List<HostOrder> dropOrders = await _context.Set<HostOrder>().Where(h =>
                h.CanRunDoppelspiel &&
                h.CanceledDate == null &&
                h.FinishedDate == null &&
                h.FailedDate == null &&
                h.Actions.Any(a => a.ActionType == ActionType.Drop && targets.Contains(a.Node) && a.IsRegisteredForDoppelspiel && a.DoppelspielPartner == null))
            .OrderBy(h => h.ConsideredForDoppelspielDate).ToListAsync();

        if (pickCount >= dropOrders.Count)
        {
            // All drops will be part of a Doppelspiel
            return null;
        }

        return dropOrders.Skip(pickCount).FirstOrDefault();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetCurrentHostOrderForAgv(Guid agv)
    {
        return await _context.Set<HostOrder>()
            .Where(h => h.IsQueued &&
                        h.AssignedAgv == agv &&
                        h.FinishedDate == null &&
                        h.FailedDate == null &&
                        h.StartedDate != null)
            .OrderByDescending(h => h.StartedDate).FirstOrDefaultAsync();
    }
}