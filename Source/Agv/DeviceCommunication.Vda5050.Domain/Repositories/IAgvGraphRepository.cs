using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="AgvGraph"/> related queries.
/// </summary>
[PublicAPI]
public interface IAgvGraphRepository
{
    /// <summary>
    /// Get the graph for an agv.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns>The agv graph.</returns>
    Task<AgvGraph> GetAgvGraphAsync(Guid agvCorrelationId);
}