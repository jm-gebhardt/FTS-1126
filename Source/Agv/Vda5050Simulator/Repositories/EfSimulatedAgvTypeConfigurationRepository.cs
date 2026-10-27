using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Vda5050Simulator.DataAccess;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Vda5050Simulator.Repositories;

public interface IAgvTypeConfigurationRepository
{
    Task<AgvTypeConfiguration?> GetAsync(Guid agvTypeId);
    
    Task<IReadOnlyCollection<AgvTypeConfiguration>> GetAllAsync();
    
    Task AddAsync(AgvTypeConfiguration configuration);
    
    Task UpdateAsync(AgvTypeConfiguration configuration);
    
    Task DeleteAsync(Guid agvTypeId);
}

public class EfAgvTypeConfigurationRepository(IDbContextFactory<Vda5050SimulatorDbContext> dbContextFactory) : IAgvTypeConfigurationRepository
{
    public async Task<AgvTypeConfiguration?> GetAsync(Guid agvTypeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<AgvTypeConfiguration>()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == agvTypeId);
    }

    public async Task<IReadOnlyCollection<AgvTypeConfiguration>> GetAllAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<AgvTypeConfiguration>()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(AgvTypeConfiguration configuration)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Set<AgvTypeConfiguration>().Add(configuration);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            throw new AgvTypeConfigurationAlreadyExistsException(configuration.AgvTypeIdentifier);
        }
    }

    public async Task UpdateAsync(AgvTypeConfiguration configuration)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Set<AgvTypeConfiguration>().Update(configuration);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AgvTypeConfigurationNotFoundException(configuration.Id);
        }
    }

    public async Task DeleteAsync(Guid agvTypeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var configuration = await context.Set<AgvTypeConfiguration>()
                                .SingleOrDefaultAsync(c => c.Id == agvTypeId)
                            ?? throw new AgvTypeConfigurationNotFoundException(agvTypeId);
        context.Set<AgvTypeConfiguration>().Remove(configuration);
        await context.SaveChangesAsync();
    }
}
