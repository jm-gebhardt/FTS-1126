using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Repository for bounding box polygons.
/// </summary>
[PublicAPI]
public interface IBoundingBoxRepository
{
    /// <summary>
    /// Replaces all stored bounding boxes with the given list.
    /// </summary>
    Task SaveAllAsync(IReadOnlyList<ResourceBoundingBox> boundingBoxes);

    /// <summary>
    /// Deletes all bounding boxes for the given agvType and inserts the given list.
    /// </summary>
    Task AddForAgvTypeAsync(string agvType, IReadOnlyList<ResourceBoundingBox> boundingBoxes);

    /// <summary>
    /// Returns bounding boxes for the given AGV type, optionally filtered to a specific resource type.
    /// </summary>
    Task<IReadOnlyList<ResourceBoundingBox>> GetByAgvTypeAsync(string agvType);

    /// <summary>
    /// Returns the bounding box for a specific resource and AGV type, or null if not found.
    /// </summary>
    Task<ResourceBoundingBox?> GetAsync(Guid resourceId, string agvType);

    /// <summary>
    /// Returns all stored bounding boxes.
    /// </summary>
    Task<IReadOnlyList<ResourceBoundingBox>> GetAllAsync();

    /// <summary>
    /// Returns the AGV types for which bounding boxes are stored.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetAgvTypesAsync();

    /// <summary>
    /// Populates the in-memory cache from the database.
    /// </summary>
    Task LoadInMemoryCache();
}