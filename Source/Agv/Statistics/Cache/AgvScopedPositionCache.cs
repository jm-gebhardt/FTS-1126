using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Memory;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Cache;

/// Represents a cache for storing and managing items isolated by AGV and it's position.
/// The cache ensures that the number of items per AGV does not exceed a specified limit, and it automatically removes
/// the oldest entries when the limit is exceeded.
public class AgvScopedPositionCache<TItem> : IAgvScopedPositionCache<TItem>
    where TItem : notnull
{
    private readonly ICache<string, List<TItem>> _inner;
    private readonly int _maxItemsPerAgv;
    private readonly ILogger<AgvScopedPositionCache<TItem>>? _logger;
    private readonly bool _warnOnOverflow;

    /// <inheritdoc cref="AgvScopedPositionCache{TItem}"/>
    public AgvScopedPositionCache(ICache<string, List<TItem>> inner,
        int maxItemsPerAgv,
        ILogger<AgvScopedPositionCache<TItem>>? logger = null,
        bool warnOnOverflow = false
    )
    {
        _inner = inner;
        _maxItemsPerAgv = maxItemsPerAgv;
        _logger = logger;
        _warnOnOverflow = warnOnOverflow;
    }

    private static string GetKey(string serial, AgvPosition position, DateTime timeStamp) => $"{serial}:{position}:{timeStamp:O}";

    /// <inheritdoc />
    public void AddItem(string agvSerialNumber, AgvPosition agvPosition, DateTime timeStamp, TItem item) => AddItem(agvSerialNumber, agvPosition, timeStamp, [item]);

    /// <inheritdoc />
    public void AddItem(string agvSerialNumber, AgvPosition agvPosition, DateTime timeStamp, List<TItem> items)
    {
        string key = GetKey(agvSerialNumber, agvPosition, timeStamp);
        if (!_inner.TryGet(key, out List<TItem>? list))
        {
            list = items;
        }
        else
        {
            list.AddRange(items);
        }

        _inner.AddOrUpdate(key, list);

        int count = _inner.Count(kvp => kvp.Key.StartsWith(agvSerialNumber + ":"));
        if (count > _maxItemsPerAgv)
        {
            _inner.RemoveOldest(kvp => kvp.Key.StartsWith(agvSerialNumber + ":"));
            if (_warnOnOverflow)
            {
                _logger?.LogWarning("AGV {Serial} position cache overflow: {Count} items, max allowed is {MaxItems}. Oldest items removed", agvSerialNumber, count, _maxItemsPerAgv);
            }
        }
    }

    /// <inheritdoc />
    public bool TryGet(string agvSerialNumber, AgvPosition agvPosition, DateTime timeStamp, [MaybeNullWhen(false)] out List<TItem> value)
    {
        return _inner.TryGet(GetKey(agvSerialNumber, agvPosition, timeStamp), out value);
    }

    /// <inheritdoc />
    public bool TryRemove(string agvSerialNumber, AgvPosition agvPosition, DateTime timeStamp, [MaybeNullWhen(false)] out List<TItem> removed)
    {
        return _inner.TryRemove(GetKey(agvSerialNumber, agvPosition, timeStamp), out removed);
    }
}