using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Registry for Cargo
/// </summary>
public class CargoRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IAgvRepository agvRepository, IConcurrentDictionaryProvider<Guid, List<Cargo>> agvCargos)
    : ICargoRepository
{
    /// <inheritdoc />
    public async Task<bool> HasAgvCargoAsync(Guid correlationId)
    {
        if (agvCargos.ConcurrentDict.ContainsKey(correlationId))
        {
            return true;
        }

        bool isLoaded = await (await dbContextFactory.CreateDbContextAsync()).Set<Cargo>()
            .AsNoTracking()
            .AnyAsync(q => q.AgvCorrelationId == correlationId);

        return isLoaded;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, bool>> HaveAgvsCargoAsync(ICollection<Guid> correlationIds)
    {
        Dictionary<Guid, bool> hasCargoByAgvCorrelationId = agvCargos.ConcurrentDict
            .Where(kvp => correlationIds.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Any());

        List<Guid> missingCorrelationIds = correlationIds.Except(hasCargoByAgvCorrelationId.Keys).ToList();

        if (missingCorrelationIds.Count > 0)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await agvRepository.ThrowIfAgvsNotExistAsync(missingCorrelationIds);
            DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

            List<Guid> agvsWithCargo = await dbSet
                .AsNoTracking()
                .Where(q => missingCorrelationIds.Contains(q.AgvCorrelationId))
                .Select(g => g.AgvCorrelationId)
                .Distinct()
                .ToListAsync();

            foreach (var agv in agvsWithCargo)
            {
                hasCargoByAgvCorrelationId[agv] = true;
                missingCorrelationIds.Remove(agv);
            }
        }

        foreach (var correlationId in missingCorrelationIds)
        {
            hasCargoByAgvCorrelationId.TryAdd(correlationId, false);
        }

        return hasCargoByAgvCorrelationId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Cargo>> GetCargosAsync(Guid correlationId)
    {
        if (agvCargos.ConcurrentDict.TryGetValue(correlationId, out List<Cargo>? cargos))
        {
            return cargos.AsReadOnly();
        }

        cargos = await (await dbContextFactory.CreateDbContextAsync()).Set<Cargo>()
            .AsNoTracking()
            .Where(q => q.AgvCorrelationId == correlationId)
            .ToListAsync();

        return cargos.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Cargo>>> GetCargosAsync(ICollection<Guid> correlationIds)
    {
        Dictionary<Guid, IReadOnlyCollection<Cargo>> cargosByAgvCorrelationId = agvCargos.ConcurrentDict
            .Where(kvp => correlationIds.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, IReadOnlyCollection<Cargo> (kvp) => kvp.Value.AsReadOnly());

        List<Guid> missingCorrelationIds = correlationIds.Except(cargosByAgvCorrelationId.Keys).ToList();

        if (missingCorrelationIds.Count > 0)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await agvRepository.ThrowIfAgvsNotExistAsync(correlationIds);
            DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

            Dictionary<Guid, IReadOnlyCollection<Cargo>> dbResults = await dbSet
                .AsNoTracking()
                .Where(q => missingCorrelationIds.Contains(q.AgvCorrelationId))
                .GroupBy(c => c.AgvCorrelationId)
                .ToDictionaryAsync(q => q.Key, q => q.ToList().AsReadOnly() as IReadOnlyCollection<Cargo>);

            foreach ((var agvCorrelationId, IReadOnlyCollection<Cargo> cargos) in dbResults)
            {
                cargosByAgvCorrelationId.TryAdd(agvCorrelationId, cargos);
                missingCorrelationIds.Remove(agvCorrelationId);
            }
        }

        foreach (var correlationId in missingCorrelationIds)
        {
            // add empty cargo collection for Agvs without cargos
            cargosByAgvCorrelationId.TryAdd(correlationId, new List<Cargo>().AsReadOnly());
        }

        return cargosByAgvCorrelationId.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<bool> SetCargoAsync(Guid agvCorrelationId, IReadOnlyCollection<Cargo> cargos)
    {
        (ICollection<Cargo> cargosToAdd, ICollection<Cargo> cargosToUpdate, ICollection<Cargo> cargosToRemove) = UpdateInMemoryCargo(agvCorrelationId, cargos);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        if (cargosToAdd.Count == 0 && cargosToUpdate.Count == 0 && cargosToRemove.Count == 0)
        {
            return false;
        }
       
        DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

        await dbSet.AddRangeAsync(cargosToAdd);
        dbSet.UpdateRange(cargosToUpdate);
        dbSet.RemoveRange(cargosToRemove);

        int affectedRows = await dbContext.SaveChangesAsync();

        return affectedRows != 0;
    }

    /// <inheritdoc />
    public async Task LoadInMemoryCache()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        foreach ((var agvCorrelationId, List<Cargo> cargos) in await dbContext.Set<Cargo>()
                     .GroupBy(c => c.AgvCorrelationId)
                     .ToDictionaryAsync(q => q.Key, q => q.ToList()))
        {
            agvCargos.ConcurrentDict[agvCorrelationId] = cargos;
        }
    }

    private (ICollection<Cargo> cargosToAdd, ICollection<Cargo> cargosToUpdate, ICollection<Cargo> cargosToRemove) UpdateInMemoryCargo(Guid agvCorrelationId, IReadOnlyCollection<Cargo> cargos)
    {
        agvCargos.ConcurrentDict.TryGetValue(agvCorrelationId, out List<Cargo>? inMemoryCargos);
       
        List<Cargo> previousCargos = inMemoryCargos ?? [];
        List<Cargo> cargosToAdd = cargos.Where(c => previousCargos.All(p => p.CorrelationId != c.CorrelationId)).ToList();
        List<Cargo> cargosToUpdate = cargos.Where(c => previousCargos.Any(p => p.CorrelationId == c.CorrelationId)).ToList();
        List<Cargo> cargosToRemove = previousCargos.Where(p => cargos.All(c => c.CorrelationId != p.CorrelationId)).ToList();
        
        agvCargos.ConcurrentDict[agvCorrelationId] = [.. cargosToAdd, ..cargosToUpdate];
        return (cargosToAdd, cargosToUpdate, cargosToRemove);
    }
}