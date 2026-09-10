using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Repositories;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ISimulatedAgvTypeConfigurationRepository"/>
/// </summary>
public static class SimulatedAgvTypeConfigurationRepositoryExtensions
{
    /// <summary>
    /// Gets a single AGV type configuration and throws when there is none.
    /// </summary>
    /// <param name="repository">The repository to read from.</param>
    /// <param name="agvTypeId">The id of the AGV type configuration.</param>
    /// <returns>The AGV type configuration.</returns>
    /// <exception cref="AgvTypeConfigurationNotFoundException">No configuration is stored for the AGV type.</exception>
    public static async Task<SimulatedAgvTypeConfiguration> GetRequiredAsync(this ISimulatedAgvTypeConfigurationRepository repository, Guid agvTypeId)
    {
        return await repository.GetAsync(agvTypeId) ?? throw new AgvTypeConfigurationNotFoundException(agvTypeId);
    }
}