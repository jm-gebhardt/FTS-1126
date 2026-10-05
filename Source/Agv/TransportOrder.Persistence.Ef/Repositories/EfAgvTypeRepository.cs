using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Repository for AGV types.
/// </summary>
public class EfAgvTypeRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : IAgvTypeRepository
{
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string agvTypeIdentifier)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<AgvType>().AsNoTracking().AnyAsync(a => a.AgvTypeIdentifier == agvTypeIdentifier);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<AgvType>().AsNoTracking().AnyAsync(a => a.Id == id);
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateAsync(AgvType agvType, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var set = dbContext.Set<AgvType>();
        var existing = await set.SingleOrDefaultAsync(a => a.AgvTypeIdentifier == agvType.AgvTypeIdentifier, cancellationToken: cancellationToken);

        if (existing is null)
        {
            set.Add(agvType);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(agvType with { Id = existing.Id });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AgvType> GetAgvTypeAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var agvType = await dbContext.Set<AgvType>().SingleOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);
        if (agvType == null)
        {
            throw new AgvTypeNotFoundException(id);
        }
        return agvType;
    }

    /// <inheritdoc/>
    public async Task<ICollection<AgvType>> GetAllAgvTypesAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Set<AgvType>().Include(t => t.LoadStates).AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
    }
}