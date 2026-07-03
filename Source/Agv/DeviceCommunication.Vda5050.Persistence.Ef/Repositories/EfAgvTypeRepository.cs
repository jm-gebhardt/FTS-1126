using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfAgvTypeRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory) : IAgvTypeRepository
{
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string agvTypeIdentifier)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<AgvType>()
            .AsNoTracking()
            .AnyAsync(a => a.AgvTypeIdentifier == agvTypeIdentifier);
    }

    /// <inheritdoc/>
    public async Task AddAsync(AgvType agvType)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Set<AgvType>().Add(agvType);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new AgvTypeAlreadyExistsException(agvType.AgvTypeIdentifier);
        }
    }
}