using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Repository for Cargo
/// </summary>
public class CargoRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IAgvRepository agvRepository, IConcurrentDictionaryProvider<Guid, List<Cargo>> agvCargos)
    : ICargoRepository
{
    /// <inheritdoc />
    public async Task<bool> HasAgvCargoAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (agvCargos.ConcurrentDict.TryGetValue(correlationId, out List<Cargo>? inMemoryCargo))
        {
            return inMemoryCargo.Count > 0;
        }

        bool isLoaded = await (await dbContextFactory.CreateDbContextAsync(cancellationToken)).Set<Cargo>()
            .AsNoTracking()
            .AnyAsync(q => q.AgvCorrelationId == correlationId, cancellationToken);

        return isLoaded;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, bool>> HaveAgvsCargoAsync(ICollection<Guid> correlationIds, CancellationToken cancellationToken = default)
    {
        Dictionary<Guid, bool> hasCargoByAgvCorrelationId = agvCargos.ConcurrentDict
            .Where(kvp => correlationIds.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count != 0);

        List<Guid> missingCorrelationIds = correlationIds.Except(hasCargoByAgvCorrelationId.Keys).ToList();

        if (missingCorrelationIds.Count > 0)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await agvRepository.ThrowIfAgvsNotExistAsync(missingCorrelationIds, cancellationToken);
            DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

            List<Guid> agvsWithCargo = await dbSet
                .AsNoTracking()
                .Where(q => missingCorrelationIds.Contains(q.AgvCorrelationId))
                .Select(g => g.AgvCorrelationId)
                .Distinct()
                .ToListAsync(cancellationToken);

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
    public async Task<IReadOnlyCollection<Cargo>> GetCargosAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (agvCargos.ConcurrentDict.TryGetValue(correlationId, out List<Cargo>? cargos))
        {
            return cargos.AsReadOnly();
        }

        cargos = await (await dbContextFactory.CreateDbContextAsync(cancellationToken)).Set<Cargo>()
            .AsNoTracking()
            .Where(q => q.AgvCorrelationId == correlationId)
            .ToListAsync(cancellationToken);

        return cargos.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Cargo>>> GetCargosAsync(ICollection<Guid> correlationIds, CancellationToken cancellationToken = default)
    {
        Dictionary<Guid, IReadOnlyCollection<Cargo>> cargosByAgvCorrelationId = agvCargos.ConcurrentDict
            .Where(kvp => correlationIds.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, IReadOnlyCollection<Cargo> (kvp) => kvp.Value.AsReadOnly());

        List<Guid> missingCorrelationIds = correlationIds.Except(cargosByAgvCorrelationId.Keys).ToList();

        if (missingCorrelationIds.Count > 0)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await agvRepository.ThrowIfAgvsNotExistAsync(correlationIds, cancellationToken);
            DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

            Dictionary<Guid, IReadOnlyCollection<Cargo>> dbResults = await dbSet
                .AsNoTracking()
                .Where(q => missingCorrelationIds.Contains(q.AgvCorrelationId))
                .GroupBy(c => c.AgvCorrelationId)
                .ToDictionaryAsync(q => q.Key, q => q.ToList().AsReadOnly() as IReadOnlyCollection<Cargo>, cancellationToken);

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
        agvCargos.ConcurrentDict.TryGetValue(agvCorrelationId, out List<Cargo>? inMemoryCargos);

        List<Cargo> previousCargos = inMemoryCargos ?? [];
        List<Cargo> cargosToAdd = cargos.Where(c => previousCargos.All(p => p.CorrelationId != c.CorrelationId)).ToList();
        List<Cargo> cargosToUpdate = cargos.Where(c => previousCargos.Any(p => p.CorrelationId == c.CorrelationId)).ToList();
        List<Cargo> cargosToRemove = previousCargos.Where(p => cargos.All(c => c.CorrelationId != p.CorrelationId)).ToList();

        if (cargosToAdd.Count == 0 && cargosToUpdate.Count == 0 && cargosToRemove.Count == 0)
        {
            return false;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        DbSet<Cargo> dbSet = dbContext.Set<Cargo>();

        dbSet.AddRange(cargosToAdd);
        foreach (var cargo in cargosToUpdate)
        {
            dbContext.Attach(cargo);
            dbContext.Entry(cargo).State = EntityState.Modified;
        }
        dbSet.RemoveRange(cargosToRemove);
        
        int affectedRows = await dbContext.SaveChangesAsync();
        agvCargos.ConcurrentDict[agvCorrelationId] = [..cargosToAdd, ..cargosToUpdate];

        return affectedRows != 0;
    }

    /// <inheritdoc />
    public async Task LoadInMemoryCache()
    {
        agvCargos.ConcurrentDict.Clear();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        foreach ((var agvCorrelationId, List<Cargo> cargos) in await dbContext.Set<Cargo>()
                     .GroupBy(c => c.AgvCorrelationId)
                     .ToDictionaryAsync(q => q.Key, q => q.ToList()))
        {
            agvCargos.ConcurrentDict[agvCorrelationId] = cargos;
        }
    }
}