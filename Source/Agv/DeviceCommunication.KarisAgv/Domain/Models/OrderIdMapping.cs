namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;

/// <summary>
/// Represents a mapping of vehicle to internal oder ids.
/// </summary>
public class OrderIdMapping
{
    /// <summary>
    /// The order id as sent to the vehicle.
    /// </summary>
    public Guid VehicleOrderId { get; init; }

    /// <summary>
    /// The order id used fts service internally.
    /// </summary>
    public Guid InternalOrderId { get; set; }

    /// <inheritdoc cref="OrderIdMapping"/>>
    public OrderIdMapping(Guid vehicleOrderId, Guid internalOrderId)
    {
        VehicleOrderId=vehicleOrderId;
        InternalOrderId=internalOrderId;
    }
}