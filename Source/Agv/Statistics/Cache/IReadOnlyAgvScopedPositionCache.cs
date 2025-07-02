using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Statistics.Cache;

/// Represents a read-only cache for managing items scoped by AGV and its associated position.
public interface IReadOnlyAgvScopedPositionCache<T>
{
    /// Attempts to retrieve the value associated with the given AGV serial and position.
    /// <param name="agvSerialNumber">The serial number of the AGV.</param>
    /// <param name="agvPosition">The position of the AGV.</param>
    /// <param name="value">The list of items associated with the AGV and position, if found; otherwise, null.</param>
    /// <returns>
    /// True if the cache contains elements for the given AGV serial number and position; otherwise, false.
    /// </returns>
    bool TryGet(string agvSerialNumber, AgvPosition agvPosition, [MaybeNullWhen(false)] out List<T> value);
}