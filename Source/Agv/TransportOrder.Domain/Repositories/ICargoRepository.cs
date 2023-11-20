using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Repository for Cargos
/// </summary>
public interface ICargoRepository
{
    /// <summary>
    /// Retrieve current cargos loaded on the Agv for given correlationId
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <returns>Empty collection if there is no cargo known for the given Agv</returns>
    Task<IReadOnlyCollection<Cargo>> GetCargosAsync(Guid correlationId);

    /// <summary>
    /// Checks if the given Agv is loaded
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <returns>True if the Agv is loaded. False if there is no cargo found.</returns>
    Task<bool> IsAgvLoadedAsync(Guid correlationId);

    /// <summary>
    /// Retrieve current cargos loaded on the Agv for given correlationIds
    /// </summary>
    /// <param name="correlationIds">The CorrelationIds of the requested Agvs</param>
    /// <returns>A list of all Agvs with a collection of potential cargos on them</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Cargo>>> GetCargosAsync(IEnumerable<Guid> correlationIds);

    /// <summary>
    /// Sets the current cargo of the Agv. This adds, updates or deletes old entries in the repository
    /// </summary>
    /// <param name="agvCorrelationId">The CorrelationId of the Agv</param>
    /// <param name="cargos">The current cargo of the Agv</param>
    /// <returns>True if the data has changed</returns>
    Task<bool> SetCargoAsync(Guid agvCorrelationId, IReadOnlyCollection<Cargo> cargos);
}