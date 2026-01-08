using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.StationCommunication.Persistence.Ef.Repositories;

/// <inheritdoc cref="IOrderRepository"/>>
public class EfOrderRepository(IDbContextFactory<StationCommunicationDbContext> contextFactory) : EfRepository<OrderReport, StationCommunicationDbContext, IDbContextFactory<StationCommunicationDbContext>>(contextFactory), IOrderRepository
{
    /// <inheritdoc cref="IOrderRepository"/>>
    public async Task StoreOrderReport(string station, Guid orderId)
    {
        var report = new OrderReport(orderId, station);
        await AddAsync(report);
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