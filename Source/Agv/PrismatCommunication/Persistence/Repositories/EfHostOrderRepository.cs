using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="IHostOrderRepository"/>>.
public class EfHostOrderRepository(PrismatCommunicationDbContext context, IDbContextFactory<PrismatCommunicationDbContext> dbContextFactory) : EfRepository<HostOrder, PrismatCommunicationDbContext>(context, true), IHostOrderRepository
{
    private readonly PrismatCommunicationDbContext _context = context;

    /// <inheritdoc cref="IHostOrderRepository"/>>
    public async Task UpdateAsync(HostOrder hostOrder)
    {
        _context.Set<HostOrder>().Update(hostOrder);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<ICollection<HostOrder>> GetLatestUnfinishedOrdersByHu(string hu, DateTime creationDate)
    {
        return await _context.Set<HostOrder>().Where(o => o.FinishedDate == null && o.CanceledDate == null && o.Hu == hu && o.CreatedDate < creationDate).ToListAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task<HostOrder?> GetLatestUnfinishedOrderByHu(string hu)
    {
        return await _context.Set<HostOrder>().Where(o => o.FinishedDate == null && o.CanceledDate == null && o.Hu == hu).OrderByDescending(o => o.CreatedDate).FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IHostOrderRepository"/>
    public async Task DeleteOutdatedOrdersAsync(DateTime outDatedTime)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(30)); // the default command timeout is 30s, which could be too short, depending on the amount of data

        var outdated = new SqlParameter("OutdatedTime", outDatedTime);

        await dbContext.Database.ExecuteSqlRawAsync("EXECUTE [dbo].[HousekeepHostOrders] @OutdatedTime", outdated);
    }
}