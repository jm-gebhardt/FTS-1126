using System.Collections.Concurrent;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IOrderRepository"/>>
public class OrderRepository: IOrderRepository
{
    private readonly ConcurrentDictionary<string, List<Guid>> _stationPickReports = new();

    /// <inheritdoc cref="IOrderRepository"/>>
    public void StorePickOrderReport(string station, Guid orderId)
    {
        _stationPickReports.AddOrUpdate(station,
            addValueFactory: (_) => [orderId],
            updateValueFactory: (_, oldValue) =>
            {
                oldValue.Add(orderId);
                return oldValue;
            });
    }

    /// <inheritdoc cref="IOrderRepository"/>>
    public void RemovePickOrderReport(string station, Guid orderId)
    {
        if (_stationPickReports.TryGetValue(station, out List<Guid>? orders))
        {
            orders.Remove(orderId);
        }
    }

    /// <inheritdoc cref="IOrderRepository"/>>
    public bool HasOrderBeenReported(string station, Guid orderId)
    {
        bool stationExists = _stationPickReports.ContainsKey(station);
        if (!stationExists)
        {
            return stationExists;
        }
        bool reportsExist = _stationPickReports.TryGetValue(station, out List<Guid>? orders);
        return reportsExist && orders != null && orders.Contains(orderId);
    }
}