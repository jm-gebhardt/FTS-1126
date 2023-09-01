using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.EntityFrameworkCore;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Domain.Repositories;
using TransportOrder.Persistence.Ef.DataAccess;

namespace TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Agv Registry using Entity Framework to access the AgvSaga persistence storage 
/// </summary>
public class EfAgvRegistry : IAgvRegistry
{
    /// <summary>
    /// Get count of all AGVs in the registry
    /// </summary>
    public int Count => AgvSagas.Count();

    /// <summary>
    /// Get count of idle AGVs in the registry
    /// </summary>
    public int CountIdle => GetIdleAgvs().Count();

    private readonly IDbContextFactory<SagaPersistenceDbContext> _dbContextFactory;

    private IQueryable<AgvSaga> AgvSagas =>
        _dbContextFactory.CreateDbContext()
            .Set<AgvSaga>()
            .AsNoTracking(); // No need to track changes

    /// <summary>
    /// Constructor of the EF Agv Registry
    /// </summary>
    /// <param name="dbContextFactory"></param>
    public EfAgvRegistry(IDbContextFactory<SagaPersistenceDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public IQueryable<IAgv> GetAllAgvs()
    {
        return AgvSagas
            .Cast<IAgv>();
    }

    /// <inheritdoc />
    public async Task<List<IAgv>> GetAllAgvsWithGraphAsync()
    {
        return await AgvSagas.Where(s => s.GraphExists).AsNoTracking()
            .Cast<IAgv>().ToListAsync();
    }

    /// <inheritdoc/>
    public IQueryable<IAgv> GetIdleAgvs()
    {
        return AgvSagas
            .IdleOnly();
    }

    /// <inheritdoc/>
    public IAgv? GetAgv(Guid? correlationId)
    {
        if (correlationId == null)
        {
            return null;
        }

        return AgvSagas
            .Where(x => x.CorrelationId == correlationId)
            .Cast<IAgv>()
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public IAgv GetRequiredAgv(Guid? correlationId)
    {
        var agv = GetAgv(correlationId);
        if (agv == null)
        {
            throw new AgvNotFoundException(correlationId);
        }

        return agv;
    }

    /// <inheritdoc />
    public async Task<bool> CheckIfAgvExistsAsync(Guid correlationId)
    {
        var saga = await AgvSagas.SingleOrDefaultAsync(a => a.CorrelationId == correlationId);
        return saga != default;
    }
}