using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Statistics.Cache;

/// Defines a cache of items isolated by AGV and its position.
public interface IAgvScopedPositionCache<T> : IReadOnlyAgvScopedPositionCache<T>
    where T : notnull
{
    /// Adds an item to the cache, scoped by a specific AGV serial number and its position.
    /// If an entry for the given AGV and position already exists, the new item is added to the existing items.
    /// <param name="agvSerialNumber">The serial number of the AGV.</param>
    /// <param name="agvPosition">The current position of the AGV.</param>
    /// <param name="timeStamp">The timestamp of the AgvPosition.</param>
    /// <param name="item">The item to add.</param>
    void AddItem(string agvSerialNumber, AgvPosition agvPosition, DateTime timeStamp, T item);

    /// Adds a collection of items to the cache, scoped by a specific AGV serial number and its position.
    /// If an entry for the given AGV and position already exists, the new items are added to the existing items.
    /// <param name="agvSerialNumber">The serial number of the AGV.</param>
    /// <param name="agvPosition">The current position of the AGV.</param>
    /// <param name="timeStamp">The timestamp of the AgvPosition.</param>
    /// <param name="items">The items to add.</param>
    void AddItem(string agvSerialNumber, AgvPosition agvPosition, DateTime timeStamp, List<T> items);

    /// Attempts to remove an entry from the cache based on the provided AGV serial number
    /// and position. If the entry is found and removed, it will return the associated list
    /// of items in the `removed` parameter and return true; otherwise, it will return false.
    /// <param name="agvSerialNumber">The serial number of the AGV.</param>
    /// <param name="agvPosition">The position of the AGV.</param>
    /// <param name="timeStamp">The timestamp of the AgvPosition.</param>
    /// <param name="removed">The list of items removed from the cache, or `null` if no entry is found.</param>
    /// <returns>True if the entry is found and removed; otherwise, false.</returns>
    bool TryRemove(string agvSerialNumber, AgvPosition agvPosition, DateTime timeStamp, [MaybeNullWhen(false)] out List<T> removed);
}