using System.Collections.Concurrent;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IOrderRepository"/>>
public class OrderRepository: IOrderRepository
{
    private readonly ConcurrentDictionary<string, List<Guid>> _stationPickReports = new();

    /// <inheritdoc cref="IOrderRepository"/>>
    public Task StoreOrderReport(string station, Guid orderId)
    {
        _stationPickReports.AddOrUpdate(station,
            addValueFactory: (_) => [orderId],
            updateValueFactory: (_, oldValue) =>
            {
                oldValue.Add(orderId);
                return oldValue;
            });
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IOrderRepository"/>>
    public Task RemoveOrderReport(string station, Guid orderId)
    {
        if (_stationPickReports.TryGetValue(station, out List<Guid>? orders))
        {
            orders.Remove(orderId);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IOrderRepository"/>>
    public async  Task<bool> HasOrderBeenReported(string station, Guid orderId)
    {
        await Task.Delay(0);
        bool stationExists = _stationPickReports.ContainsKey(station);
        if (!stationExists)
        {
            return stationExists;
        }
        bool reportsExist = _stationPickReports.TryGetValue(station, out List<Guid>? orders);
        return reportsExist && orders != null && orders.Contains(orderId);
    }
}