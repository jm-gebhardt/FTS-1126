using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="AgvType"/>.
/// </summary>
public interface IAgvTypeRepository
{
    /// <summary>
    /// Checks whether an AGV type with the given identifier exists.
    /// </summary>
    /// <param name="agvTypeIdentifier">The AGV type identifier.</param>
    /// <returns>True if an AGV type with the identifier exists.</returns>
    public Task<bool> ExistsAsync(string agvTypeIdentifier);

    /// <summary>
    /// Get an agv type by its identifier.
    /// </summary>
    /// <param name="agvTypeIdentifier">The AGV type identifier.</param>
    /// <returns>The agv type, if it exists, else null.</returns>
    public Task<AgvType?> GetAgvTypeAsync(string agvTypeIdentifier);

    /// <summary>
    /// Get a dictionary of agv type identifiers by their identifiers.
    /// </summary>
    /// <returns>A dictionary of AGV types keyed by their identifiers.</returns>
    public Task<Dictionary<string, Guid>> GetAgvTypeIdentifiersAsync();

    /// <summary>
    /// Adds a new AGV type.
    /// </summary>
    /// <param name="agvType">The AGV type to add.</param>
    public Task AddAsync(AgvType agvType);
}