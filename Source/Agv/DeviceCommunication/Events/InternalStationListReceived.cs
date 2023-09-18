using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Events;

/// <summary>
/// Transformation of <see cref="StationList"/> into MassTransit message.
/// </summary>
[PublicAPI]
public record InternalStationListReceived : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// See <see cref="StationList"/>.
    /// </summary>
    public MqttStationList StationList { get; }

    /// <summary>
    /// Build station list message.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="stationList"></param>
    public InternalStationListReceived(Guid correlationId, MqttStationList stationList)
    {
        CorrelationId = correlationId;
        StationList = stationList;
    }
}