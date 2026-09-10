using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="SimulatedAgvTypeConfiguration"/>.
/// </summary>
public interface ISimulatedAgvTypeConfigurationRepository
{
    /// <summary>
    /// Gets a single AGV type configuration.
    /// </summary>
    /// <param name="agvTypeId">The id of the AGV type configuration.</param>
    /// <returns>The AGV type configuration, if it exists, else null.</returns>
    public Task<SimulatedAgvTypeConfiguration?> GetAsync(Guid agvTypeId);

    /// <summary>
    /// Gets all AGV type configurations.
    /// </summary>
    /// <returns>All stored AGV type configurations.</returns>
    public Task<IReadOnlyCollection<SimulatedAgvTypeConfiguration>> GetAllAsync();

    /// <summary>
    /// Adds a new AGV type configuration.
    /// </summary>
    /// <param name="configuration">The AGV type configuration to add.</param>
    public Task AddAsync(SimulatedAgvTypeConfiguration configuration);

    /// <summary>
    /// Replaces an existing AGV type configuration.
    /// </summary>
    /// <param name="configuration">The AGV type configuration to store.</param>
    public Task UpdateAsync(SimulatedAgvTypeConfiguration configuration);

    /// <summary>
    /// Deletes an AGV type configuration.
    /// </summary>
    /// <param name="agvTypeId">The id of the AGV type configuration.</param>
    public Task DeleteAsync(Guid agvTypeId);
}