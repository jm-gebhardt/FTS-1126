using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.DataAccess;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Repositories;

/// <inheritdoc/>
public class EfSimulatedAgvTypeConfigurationRepository(IDbContextFactory<Vda5050SimulatorDbContext> dbContextFactory) : ISimulatedAgvTypeConfigurationRepository
{
    /// <inheritdoc/>
    public async Task<SimulatedAgvTypeConfiguration?> GetAsync(Guid agvTypeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<SimulatedAgvTypeConfiguration>()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == agvTypeId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<SimulatedAgvTypeConfiguration>> GetAllAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<SimulatedAgvTypeConfiguration>()
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task AddAsync(SimulatedAgvTypeConfiguration configuration)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Set<SimulatedAgvTypeConfiguration>()
            .Add(configuration);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            throw new AgvTypeConfigurationAlreadyExistsException(configuration.AgvTypeIdentifier);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SimulatedAgvTypeConfiguration configuration)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Set<SimulatedAgvTypeConfiguration>()
            .Update(configuration);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AgvTypeConfigurationNotFoundException(configuration.Id);
        }
    }

    /// <inheritdoc/>
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