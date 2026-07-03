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
    /// Adds a new AGV type.
    /// </summary>
    /// <param name="agvType">The AGV type to add.</param>
    public Task AddAsync(AgvType agvType);
}