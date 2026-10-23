using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Vda5050Simulator.DataAccess;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Vda5050Simulator.Repositories;

public interface ISimulatedAgvTypeConfigurationRepository
{
    Task<SimulatedAgvTypeConfiguration?> GetAsync(Guid agvTypeId);
    
    Task<IReadOnlyCollection<SimulatedAgvTypeConfiguration>> GetAllAsync();
    
    Task AddAsync(SimulatedAgvTypeConfiguration configuration);
    
    Task UpdateAsync(SimulatedAgvTypeConfiguration configuration);
    
    Task DeleteAsync(Guid agvTypeId);
}

public class EfSimulatedAgvTypeConfigurationRepository(IDbContextFactory<Vda5050SimulatorDbContext> dbContextFactory) : ISimulatedAgvTypeConfigurationRepository
{
    public async Task<SimulatedAgvTypeConfiguration?> GetAsync(Guid agvTypeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<SimulatedAgvTypeConfiguration>()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == agvTypeId);
    }

    public async Task<IReadOnlyCollection<SimulatedAgvTypeConfiguration>> GetAllAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<SimulatedAgvTypeConfiguration>()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(SimulatedAgvTypeConfiguration configuration)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Set<SimulatedAgvTypeConfiguration>().Add(configuration);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            throw new AgvTypeConfigurationAlreadyExistsException(configuration.AgvTypeIdentifier);
        }
    }

    public async Task UpdateAsync(SimulatedAgvTypeConfiguration configuration)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Set<SimulatedAgvTypeConfiguration>().Update(configuration);
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
        var configuration = await context.Set<SimulatedAgvTypeConfiguration>()
                                .SingleOrDefaultAsync(c => c.Id == agvTypeId)
                            ?? throw new AgvTypeConfigurationNotFoundException(agvTypeId);
        context.Set<SimulatedAgvTypeConfiguration>().Remove(configuration);
        await context.SaveChangesAsync();
    }
}
