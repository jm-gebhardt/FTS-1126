using Gebhardt.Acc.WCSCommunication.Domain.Exceptions;
using Gebhardt.Acc.WCSCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.WCSCommunication.Domain.Repositories;
using Gebhardt.Acc.WCSCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.WCSCommunication.Persistence.Repositories;

/// <summary>
/// Provides a repository for host order related queries.
/// </summary>
public class HostOrderRepository : IHostOrderRepository
{
    /// <summary>
    /// Gets the underlying db set.
    /// </summary>
    private readonly IQueryable<HostOrder> _dbSet;

    private readonly WcsCommunicationDbContext _context;

    /// <inheritdoc cref="HostOrderRepository"/>>
    public HostOrderRepository(WcsCommunicationDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<HostOrder>();
    }

    /// <inheritdoc/>
    public async Task AddAsync(HostOrder hostOrder)
    {
        await _context.AddAsync(hostOrder);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<HostOrder> GetByIdAsync(Guid correlationId)
    {
        var order = await _dbSet.SingleOrDefaultAsync(o => o.CorrelationId == correlationId);
        if (order == null)
        {
            throw new HostOrderNotFoundException(correlationId);
        }
        return order;
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(HostOrder hostOrder)
    {
        _context.Remove(hostOrder);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(HostOrder hostOrder)
    {
        if (!_context.ChangeTracker.AutoDetectChangesEnabled)
        {
            _context.HostOrder.Update(hostOrder);
        }
        await _context.SaveChangesAsync();
    }
}