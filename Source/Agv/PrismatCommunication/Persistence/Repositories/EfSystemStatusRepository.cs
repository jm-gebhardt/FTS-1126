using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="ISystemStatusRepository"/>>
public class EfSystemStatusRepository(PrismatCommunicationDbContext context) : EfRepository<SystemStatus, PrismatCommunicationDbContext>(context), ISystemStatusRepository
{
    private readonly PrismatCommunicationDbContext _context = context;

    /// <inheritdoc cref="ISystemStatusRepository"/>>
    public async Task<SystemStatus?> GetStatus()
    {
        return await _context.Set<SystemStatus>().SingleOrDefaultAsync();
    }

    /// <inheritdoc cref="ISystemStatusRepository"/>>
    public async Task UpdateAsync(SystemStatus status)
    {
        _context.Set<SystemStatus>().Update(status);
        await _context.SaveChangesAsync();
    }
}