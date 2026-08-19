using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class EfBoundingBoxRepository : IBoundingBoxRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly ICache<string, IReadOnlyList<ResourceBoundingBox>> _cache;

    /// <inheritdoc cref="EfBoundingBoxRepository"/>
    public EfBoundingBoxRepository(
        IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        ICache<string, IReadOnlyList<ResourceBoundingBox>> cache)
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task SaveAllAsync(IReadOnlyList<ResourceBoundingBox> boundingBoxes)
    {
        IReadOnlyCollection<string> oldKeys = _cache.GetKeys();

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Set<ResourceBoundingBox>().ExecuteDeleteAsync();
        await context.Set<ResourceBoundingBox>().AddRangeAsync(boundingBoxes);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        HashSet<string> newTypes = UpdateCache(boundingBoxes);
        foreach (string stale in oldKeys.Where(k => !newTypes.Contains(k)))
        {
            _cache.TryRemove(stale, out _);
        }
    }

    /// <inheritdoc />
    public async Task AddForAgvTypeAsync(string agvType, IReadOnlyList<ResourceBoundingBox> boundingBoxes)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Set<ResourceBoundingBox>().Where(b => b.AgvType == agvType).ExecuteDeleteAsync();
        await context.Set<ResourceBoundingBox>().AddRangeAsync(boundingBoxes);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        _cache.AddOrUpdate(agvType, boundingBoxes);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ResourceBoundingBox>> GetByAgvTypeAsync(string agvType)
    {
        if (_cache.TryGet(agvType, out IReadOnlyList<ResourceBoundingBox>? cached))
        {
            return Task.FromResult(cached);
        }

        return GetByAgvTypeFromDbAsync(agvType);
    }

    /// <inheritdoc />
    public async Task<ResourceBoundingBox?> GetAsync(Guid resourceId, string agvType)
    {
        IReadOnlyList<ResourceBoundingBox> boxes = await GetByAgvTypeAsync(agvType);
        return boxes.FirstOrDefault(b => b.ResourceId == resourceId);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ResourceBoundingBox>> GetAllAsync()
    {
        IReadOnlyCollection<string> keys = _cache.GetKeys();
        IReadOnlyList<ResourceBoundingBox> all = _cache.GetValues(keys).SelectMany(x => x).ToList();
        return Task.FromResult(all);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<string>> GetAgvTypesAsync()
    {
        return Task.FromResult(_cache.GetKeys());
    }

    /// <inheritdoc />
    public async Task LoadInMemoryCache()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<ResourceBoundingBox> all = await context.Set<ResourceBoundingBox>().ToListAsync();
        UpdateCache(all);
    }

    private HashSet<string> UpdateCache(IEnumerable<ResourceBoundingBox> boundingBoxes)
    {
        Dictionary<string, List<ResourceBoundingBox>> boundingBoxesByType = boundingBoxes
            .GroupBy(b => b.AgvType)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach ((string agvType, List<ResourceBoundingBox> boxes) in boundingBoxesByType)
        {
            _cache.AddOrUpdate(agvType, boxes);
        }

        return boundingBoxesByType.Keys.ToHashSet();
    }

    private async Task<IReadOnlyList<ResourceBoundingBox>> GetByAgvTypeFromDbAsync(string agvType)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<ResourceBoundingBox> query = context.Set<ResourceBoundingBox>().Where(b => b.AgvType == agvType);

        return await query.ToListAsync();
    }
}