using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;

/// <summary>
/// Represents an agv that went offline in reserved zones.
/// </summary>
[PublicAPI]
public class OfflineAgvInZones
{
    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid AgvCorrelationId { get; }

    /// <summary>
    /// The serial number of the agv.
    /// </summary>
    public string SerialNumber { get; }

    /// <summary>
    /// The zones the agv is currently in.
    /// </summary>
    public HashSet<string> CurrentZones { get; private set; }

    /// <summary>
    /// The zones the agv was in when it went offline.
    /// </summary>
    public HashSet<string> OriginalZones { get; }

    /// <summary>
    /// The the agv went offline.
    /// </summary>
    public DateTime OfflineTimestamp { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="serialNumber"></param>
    /// <param name="zones"></param>
    /// <param name="offlineTimestamp"></param>
    public OfflineAgvInZones(Guid agvCorrelationId, string serialNumber,
        ICollection<string> zones, DateTime offlineTimestamp)
    {
        AgvCorrelationId = agvCorrelationId;
        SerialNumber = serialNumber;
        CurrentZones = [..zones];
        OriginalZones = [..zones];
        OfflineTimestamp = offlineTimestamp;
    }

    /// <summary>
    /// Update the zones.
    /// </summary>
    /// <param name="newZones">New zones.</param>
    /// <param name="removedZones">Removed zones.</param>
    /// <returns></returns>
    public ICollection<string> UpdateZones(ICollection<string> newZones, out ICollection<string> removedZones)
    {
        var newZoneSet = new HashSet<string>(newZones);
        removedZones = CurrentZones.Except(newZoneSet).ToList();

        CurrentZones = newZoneSet;
        return removedZones;
    }

    /// <summary>
    /// True, if the agv is in any zones.
    /// </summary>
    public bool IsInAnyZone => CurrentZones.Any();
}