using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Services;

/// <summary>
/// Provides agv states for refreshing the zones of offline agvs.
/// </summary>
public interface IZoneRefreshStateService
{
   /// <summary>
   /// Add offline agv in zones.
   /// </summary>
   /// <param name="correlationId">The correlation id of the agv.</param>
   /// <param name="serialNumber">The serial number of the offline agv.</param>
   /// <param name="zones">The zones the agv reserved.</param>
   /// <param name="currentOnlineAgvs">Correlation ids of currently online agvs.</param>
    Task AddOfflineAgvInZones(Guid correlationId, string serialNumber, ICollection<string> zones, ICollection<string> currentOnlineAgvs);

    /// <summary>
    /// Set agv back online.
    /// </summary>
    /// <param name="correlationId">The correlation id of the agv.</param>
    Task SetAgvBackOnline(Guid correlationId);

    /// <summary>
    /// Update agvs from refreshed zones.
    /// </summary>
    /// <param name="zones">The zones received.</param>
    /// <returns>The zone an agv was removed from by agv.</returns>
    Task<Dictionary<Guid, ICollection<string>>> UpdateAgvsFromRefreshedZones(ICollection<MqttZone> zones);

    /// <summary>
    /// Get the serial number of the next online agv to request zones from.
    /// </summary>
    /// <returns>The serial number of an online agv.</returns>
    string? GetNextOnlineAgv();

    /// <summary>
    /// Get all agvs that are offline in zones.
    /// </summary>
    /// <returns>Collection of agvs offline in zones.</returns>
    ICollection<OfflineAgvInZones> OfflineAgvsInZones { get; }

    /// <summary>
    /// True, if a zone refresh should be requested.
    /// </summary>
    bool ZoneRefreshRequired { get; }
}