using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Dto for battery status of an AGV
/// </summary>
public record AgvBatteryStatusDto : DatedMessage
{
    /// <summary>The CorrelationId of the Agv</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>The current SOC [%]</summary>
    public double BatteryCharge { get; init; }

    /// <inheritdoc cref="AgvBatteryStatusDto"/>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="batteryCharge">The current SOC [%]</param>
    public AgvBatteryStatusDto([PublicAPI] Guid correlationId, double batteryCharge)
    {
        CorrelationId = correlationId;
        BatteryCharge = batteryCharge;
    }

    /// <summary>
    /// AutoMapper ctor
    /// </summary>
    private AgvBatteryStatusDto()
    {
    }
}