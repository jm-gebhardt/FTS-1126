using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Provides the bounding boxes of all graph resources per AGV type. Caches data in memory.
/// </summary>
[PublicAPI]
public interface IBoundingBoxService
{
    /// <summary>
    /// Returns the bounding boxes of all graph resources for the given AGV type, calculating them when they are not cached yet.
    /// </summary>
    Task<IReadOnlyList<ResourceBoundingBox>> GetByAgvTypeAsync(string agvTypeIdentifier);

    /// <summary>
    /// Returns the bounding box of a single resource for the given AGV type (calculating them when they are not cached yet), or null when there is none.
    /// </summary>
    Task<ResourceBoundingBox?> GetAsync(Guid resourceId, string agvTypeIdentifier);

    /// <summary>
    /// Recalculates the bounding boxes of every persisted graph for every known AGV type and discard those of vanished types.
    /// </summary>
    Task RecalculateAllAsync();
}