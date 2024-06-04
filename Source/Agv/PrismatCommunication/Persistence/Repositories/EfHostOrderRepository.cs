using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="IHostOrderRepository"/>>.
public class EfHostOrderRepository(PrismatCommunicationDbContext context) : EfRepository<HostOrder, PrismatCommunicationDbContext>(context), IHostOrderRepository
{
    private readonly PrismatCommunicationDbContext _context = context;

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task UpdateAsync(HostOrder hostOrder)
    {
        _context.Set<HostOrder>().Update(hostOrder);
        await _context.SaveChangesAsync();
    }
}