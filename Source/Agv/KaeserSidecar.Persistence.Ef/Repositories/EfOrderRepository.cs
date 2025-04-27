using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IOrderRepository"/>>
public class EfOrderRepository(IDbContextFactory<KaeserSidecarDbContext> contextFactory) : EfRepository<OrderReport, KaeserSidecarDbContext, IDbContextFactory<KaeserSidecarDbContext>>(contextFactory), IOrderRepository
{
    /// <inheritdoc cref="IOrderRepository"/>>
    public async Task StoreOrderReport(string station, Guid orderId)
    {
        var report = new OrderReport(orderId, station);
        await AddAsync(report);
    }

    /// <inheritdoc cref="IOrderRepository"/>>
    public async Task RemoveOrderReport(string station, Guid orderId)
    {
        var report = new OrderReport(orderId, station);
        await RemoveAsync(report);
    }

    /// <inheritdoc cref="IOrderRepository"/>>
    public async Task<bool> HasOrderBeenReported(string station, Guid orderId)
    {
        try
        {
            var report = await GetByIdAsync(orderId);
            return report.Station == station;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}