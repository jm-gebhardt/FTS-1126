using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for instant action related queries.
/// </summary>
public interface IAgvInstantActionDefinitionRepository
{
    /// <summary>
    /// Adds the instant action definition.
    /// </summary>
    /// <param name="instantActionDefinition">The instant action definition.</param>
    Task<bool> Add(InstantActionDefinition instantActionDefinition);

    /// <summary>
    /// Get an instant action definition by its correlation id.
    /// </summary>
    /// <param name="correlationId">The correlation id of the instant action definition.</param>
    /// <returns>The instant action definition, if available.</returns>
    Task<InstantActionDefinition?> GetInstantActionDefinition(Guid correlationId);

    /// <summary>
    /// Updates the instant action definition.
    /// </summary>
    /// <param name="instantActionDefinition">The instant action definition.</param>
    Task<bool> Update(InstantActionDefinition instantActionDefinition);

    /// <summary>
    /// Get instant action definitions for an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <returns>The instant action definitions.</returns>
    Task<ICollection<InstantActionDefinition>> GetInstantActionDefinitionsForAgv(Guid agvCorrelationId);

    /// <summary>
    /// Get instant action definitions for each given agv.
    /// </summary>
    /// <param name="agvCorrelationIds">The agv correlation ids.</param>
    /// <returns>The instant action definitions for each given agv.</returns>
    Task<Dictionary<Guid, List<InstantActionDefinition>>> GetInstantActionDefinitionsByAgv(ICollection<Guid> agvCorrelationIds);
}