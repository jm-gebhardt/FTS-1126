using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when a map does not exist.
/// </summary>
public class MapNotFoundException : NotFoundException
{
    /// <inheritdoc cref="MapNotFoundException"/>
    /// <param name="mapId">The id of the map.</param>
    public MapNotFoundException(Guid mapId)
        : base(nameof(Map), "MapId", mapId)
    {
    }
}