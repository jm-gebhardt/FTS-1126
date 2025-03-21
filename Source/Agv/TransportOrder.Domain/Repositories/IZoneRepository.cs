using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="Zone"/> related queries.
/// </summary>
public interface IZoneRepository
{
    /// <summary>
    /// Add or update a zone.
    /// </summary>
    /// <param name="zone">The zone.</param>
    Task AddOrUpdateZone(Zone zone);

    /// <summary>
    /// Get all zones for given zone ids.
    /// </summary>
    /// <param name="zoneIds">The zone ids.</param>
    /// <returns>A collection of zones.</returns>
    Task<ICollection<Zone>> GetZones(string[] zoneIds);
}