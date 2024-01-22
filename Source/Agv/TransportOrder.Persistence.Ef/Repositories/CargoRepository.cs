using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Registry for Cargo
/// </summary>
public class CargoRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IAgvRepository agvRepository)
    : ICargoRepository
{
    /// <inheritdoc />
    public async Task<bool> HasAgvCargoAsync(Guid correlationId)
    {
        bool isLoaded = await (await dbContextFactory.CreateDbContextAsync()).Set<Cargo>()
            .AsNoTracking()
            .AnyAsync(q => q.AgvCorrelationId == correlationId);

        return isLoaded;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, bool>> HaveAgvsCargoAsync(ICollection<Guid> correlationIds)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await agvRepository.ThrowIfAgvsNotExistAsync(correlationIds);
        DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

        Dictionary<Guid, bool> hasCargoByAgvCorrelationId = await dbSet
            .AsNoTracking()
            .Where(q => correlationIds.Contains(q.AgvCorrelationId))
            .GroupBy(c => c.AgvCorrelationId)
            .ToDictionaryAsync(q => q.Key, _ => true);

        foreach (var correlationId in correlationIds)
        {
            // add hasCargo = false for Agvs without cargos
            hasCargoByAgvCorrelationId.TryAdd(correlationId, false);
        }

        return hasCargoByAgvCorrelationId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Cargo>> GetCargosAsync(Guid correlationId)
    {
        List<Cargo> cargos = await (await dbContextFactory.CreateDbContextAsync()).Set<Cargo>()
            .AsNoTracking()
            .Where(q => q.AgvCorrelationId == correlationId)
            .ToListAsync();

        return cargos.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Cargo>>> GetCargosAsync(ICollection<Guid> correlationIds)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await agvRepository.ThrowIfAgvsNotExistAsync(correlationIds);
        DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

        Dictionary<Guid, IReadOnlyCollection<Cargo>> cargosByAgvCorrelationId = await dbSet
            .AsNoTracking()
            .Where(q => correlationIds.Contains(q.AgvCorrelationId))
            .GroupBy(c => c.AgvCorrelationId)
            .ToDictionaryAsync(q => q.Key, q => q.ToList().AsReadOnly() as IReadOnlyCollection<Cargo>);

        foreach (var correlationId in correlationIds)
        {
            // add empty cargo collection for Agvs without cargos
            cargosByAgvCorrelationId.TryAdd(correlationId, new List<Cargo>().AsReadOnly());
        }

        return cargosByAgvCorrelationId;
    }

    /// <inheritdoc />
    public async Task<bool> SetCargoAsync(Guid agvCorrelationId, IReadOnlyCollection<Cargo> cargos)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

        List<Cargo> previousCargos = await dbSet
            .Where(existingCargo => existingCargo.AgvCorrelationId == agvCorrelationId)
            .ToListAsync();

        IEnumerable<Cargo> cargosToAdd = cargos.Where(c => previousCargos.All(p => p.CorrelationId == c.CorrelationId));
        IEnumerable<Cargo> cargosToUpdate = previousCargos.Where(p => cargos.Any(c => c.CorrelationId == p.CorrelationId));
        IEnumerable<Cargo> cargosToRemove = previousCargos.Where(p => cargos.All(c => c.CorrelationId != p.CorrelationId));

        await dbSet.AddRangeAsync(cargosToAdd);
        dbSet.UpdateRange(cargosToUpdate);
        dbSet.RemoveRange(cargosToRemove);

        int affectedRows = await dbContext.SaveChangesAsync();

        return affectedRows != 0;
    }
}