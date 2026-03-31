using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

/// <summary>
/// Repository for <see cref="GraphLayout"/> data.
/// </summary>
[PublicAPI]
public interface IGraphLayoutRepository
{
    /// <summary>
    /// Merge a collection of graph layouts into the repository.
    /// </summary>
    /// <param name="layouts">The new graph layouts.</param>
    Task StoreGraphLayoutsAsync(ICollection<GraphLayout> layouts);

    /// <summary>
    /// Get the latest layout version for a given LayoutId (by <see cref="GraphLayout.StoredAt"/>).
    /// </summary>
    /// <param name="layoutId">The layout identifier.</param>
    /// <returns>The latest layout or null if none stored.</returns>
    Task<GraphLayout?> GetLatestLayoutAsync(string layoutId);

    /// <summary>
    /// Get all latest layouts (one per LayoutId, determined by <see cref="GraphLayout.StoredAt"/>).
    /// </summary>
    Task<ICollection<GraphLayout>> GetAllLatestLayoutsAsync();

    /// <summary>
    /// Check if any stored layout contains the given vehicle type.
    /// </summary>
    /// <param name="vehicleTypeId">The vehicle type identifier.</param>
    Task<bool> HasVehicleTypeAsync(string vehicleTypeId);

    /// <summary>
    /// Get all distinct vehicle type Ids across the latest layouts.
    /// </summary>
    Task<HashSet<string>> GetStoredVehicleTypeIdsAsync();

    /// <summary>
    /// Get the latest layouts that contain the given vehicle type.
    /// </summary>
    /// <param name="vehicleTypeId">The vehicle type identifier.</param>
    Task<ICollection<GraphLayout>> GetLayoutsForVehicleTypeAsync(string vehicleTypeId);
}