using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

public class AgvRepository : IAgvRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <summary>
    ///Initializes a new <see cref="AgvRepository"/> instance.
    /// </summary>
    public AgvRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private IQueryable<Domain.Models.Agv.Agv> Agvs =>
        _dbContextFactory.CreateDbContext()
            .Set<Domain.Models.Agv.Agv>()
            .AsNoTracking(); // No need to track changes
   
    public int Count => Agvs.Count();

    public async Task<ICollection<Domain.Models.Agv.Agv>> GetAllAgvsAsync()
    {
        return await Agvs.ToListAsync();
    }

    public async Task<ICollection<Domain.Models.Agv.Agv>> GetAllAgvsWithGraphAsync()
    {
        return await Agvs.Where(a => a.GraphExists).ToListAsync();
    }

    public async Task<ICollection<Domain.Models.Agv.Agv>> GetIdleAgvsAsync()
    {
        return await Agvs.Where(x => x.CurrentAgvState == AgvState.Idle).ToListAsync();
    }


    public async Task<Domain.Models.Agv.Agv> GetAgvAsync(Guid correlationId)
    {
        var agv = await Agvs.Where(a => a.CorrelationId == correlationId).SingleOrDefaultAsync();
        if (agv == null)
        {
            throw new AgvNotFoundException(correlationId);
        }
        return agv;
    }


    public async Task<bool> CheckIfAgvExistsAsync(Guid correlationId)
    {
        return await Agvs.AnyAsync(a => a.CorrelationId == correlationId);
    }

    public async Task<ICollection<Domain.Models.Agv.Agv>> SelectExistingAgvsAsync(List<Guid> agvCorrelationIds)
    {
        return await Agvs.Where(a => !agvCorrelationIds.Any() || agvCorrelationIds.Contains(a.CorrelationId)).OrderBy(a => a.AgvId).ToListAsync();
    }

    public async Task<bool> UpdateAgvAsync(Domain.Models.Agv.Agv agv)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        DbSet<Domain.Models.Agv.Agv> dbSet = dbContext.Set<Domain.Models.Agv.Agv>();
        dbSet.Update(agv);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> AddAgvAsync(Domain.Models.Agv.Agv agv)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        DbSet<Domain.Models.Agv.Agv> dbSet = dbContext.Set<Domain.Models.Agv.Agv>();
        dbSet.Add(agv);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<ICollection<Domain.Models.Agv.Agv>> GetChargingAgvsWithGoodBatteryLevelAsync(int goodBatteryLevel)
    {
        return await Agvs.Where(a => a.GraphExists &&
                                     a.CurrentAgvState == AgvState.Charging &&
                                     a.AgvStatus!.BatteryCharge >= goodBatteryLevel).ToListAsync();
    }

    public async Task<ICollection<Domain.Models.Agv.Agv>> GetReadyAgvsAsync()
    {
        return await Agvs.Where(Domain.Models.Agv.Agv.IsReadyStatic).Where(a => a.GraphExists && !a.IsLocked && a.CurrentAgvState == AgvState.Idle).ToListAsync();
    }

    public async Task<string?> GetLastNodeIdAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        string? lastNodeId = await Agvs
            .Where(s => s.CorrelationId == agvCorrelationId && s.AgvStatus != null)
            .Select(s => s.AgvStatus!.LastNodeId)
            .FirstOrDefaultAsync();

        return lastNodeId;
    }

    public async Task<IReadOnlyDictionary<Guid, string?>> GetLastNodeIdAsync(ICollection<Guid> agvCorrelationIds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        Dictionary<Guid, string?> lastNodeIdsById = await Agvs
            .Where(s => agvCorrelationIds.Contains(s.CorrelationId) && s.AgvStatus != null)
            .Select(s => new { s.CorrelationId, s.AgvStatus!.LastNodeId })
            .ToDictionaryAsync(x => x.CorrelationId, x => x.LastNodeId);

        return lastNodeIdsById;
    }
}