using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

/// <summary>
/// Repository for Lif-related queries.
/// </summary>
[PublicAPI]
public interface ILifRepository
{
    /// <summary>
    /// Add a lif file.
    /// </summary>
    /// <param name="file">The lif file.</param>
    /// <param name="addedLayouts">The layouts to add.</param>
    /// <param name="deletedLayouts">The layouts to delete.</param>
    Task<bool> AddFileAsync(LifFile file, ICollection<Layout> addedLayouts, ICollection<Layout> deletedLayouts);

    /// <summary>
    /// Update lif layouts
    /// </summary>
    /// <param name="addedLayouts">The layouts to add.</param>
    /// <param name="deletedLayouts">The layouts to delete.</param>
    Task UpdateLayouts(ICollection<Layout> addedLayouts, ICollection<Layout> deletedLayouts);
    
    /// <summary>
    /// Get all lif files.
    /// </summary>
    /// <returns>Collection of lif files.</returns>
    Task<ICollection<LifFile>> GetAllFilesAsync();

    /// <summary>
    /// Get a lif file.
    /// </summary>
    /// <param name="id">The id of the lif file.</param>
    /// <returns>Collection of lif files.</returns>
    /// <exception cref="LifFileNotFoundException">Raised, when the id does not match any stored lif files.</exception>
    Task<LifFile> GetFileAsync(Guid id);

    /// <summary>
    /// Delete lif file and related layouts.
    /// </summary>
    /// <param name="id">The id of the lif file.</param>
    Task<bool> DeleteLifAsync(Guid id);

    /// <summary>
    /// Add lif layouts.
    /// </summary>
    /// <param name="layouts">The layouts to add.</param>
    Task AddLayouts(ICollection<Layout> layouts);

    /// <summary>
    /// Get lif layouts by their ids, returned in the order of the provided ids.
    /// </summary>
    /// <param name="layoutIds">The ordered ids of the layouts to retrieve.</param>
    Task<ICollection<Layout>> GetLayoutsByIdsAsync(ICollection<Guid> layoutIds);

    /// <summary>
    /// Get lif that mention the given vehicle type ids.
    /// </summary>
    /// <param name="vehicleTypeIds">The vehicle type ids.</param>
    Task<(ICollection<LifFile> lifs, ICollection<Layout> layouts)> GetLifsByVehicleTypeIdsAsync(string[] vehicleTypeIds);

    /// <summary>
    /// Get lif layouts that mention the given vehicle type id.
    /// </summary>
    /// <param name="vehicleTypeId">The vehicle type ids.</param>
    Task<ICollection<Layout>> GetLayoutsByVehicleTypeIdAsync(string vehicleTypeId);
}