using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;

/// <summary>
/// Provides <see cref="AgvGraph"/> filtered by an AGV's vehicle type.
/// </summary>
[PublicAPI]
public interface IAgvGraphProvider
{
    /// <summary>
    /// Generate a domain <see cref="AgvGraph"/> for a specific AGV from stored layout data.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation Id of the AGV.</param>
    /// <returns>The generated graph or null if no matching layout data exists.</returns>
    Task<AgvGraph?> GetAgvGraphAsync(Guid agvCorrelationId);

    /// <summary>
    /// Generate a domain <see cref="AgvGraph"/> for a specific AGV, throws if not possible.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation Id of the AGV.</param>
    /// <returns>The generated graph.</returns>
    /// <exception cref="AgvGraphNotFoundException">Thrown when no matching layout data exists.</exception>
    Task<AgvGraph> GetRequiredAgvGraphAsync(Guid agvCorrelationId);
}