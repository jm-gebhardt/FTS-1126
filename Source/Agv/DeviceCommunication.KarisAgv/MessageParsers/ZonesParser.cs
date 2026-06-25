using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Exceptions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;

/// <summary>
/// Provides a parser for <see cref="MqttZones"/>.
/// </summary>
public class ZonesParser(MqttZones zones)
{
    /// <summary>
    /// Returns true when the serial number is present otherwise false.
    /// </summary>
    [PublicAPI]
    public bool SerialNumberValid => !string.IsNullOrEmpty(zones.SerialNumber);

    /// <summary>
    /// Returns the serial number of the AGV.
    /// </summary>
    /// <exception cref="InvalidOperationException">Serial number is missing.</exception>
    [PublicAPI]
    public string SerialNumber
    {
        get
        {
            if (!SerialNumberValid)
            {
                throw new SerialNumberNotSetException(nameof(MqttZones));
            }
            return zones.SerialNumber;
        }
    }

    /// <summary>
    /// The agv zones.
    /// </summary>
    public ICollection<AgvZone> AgvZones =>
        zones.Zones.Select(z => new AgvZone(z.ZoneId,
            z.Capacity,
            z.Polygon.Select(p => new PolygonPoint(p.X, p.Y)).ToList(),
            z.Occupancy)).ToList();
}
