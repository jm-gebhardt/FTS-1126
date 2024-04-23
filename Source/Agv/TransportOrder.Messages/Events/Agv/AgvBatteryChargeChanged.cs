using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the battery charge of an agv has changed.
/// </summary>
public record AgvBatteryChargeChanged : CorrelatedBy<Guid>
{
    /// <summary>The agv correlation id.</summary>
    [UsedImplicitly]
    public Guid CorrelationId => AgvBatteryStatus.CorrelationId;

    /// <summary>Battery charge of the agv before the change.</summary>
    [UsedImplicitly]
    public double OldBatteryCharge { get; init; }

    /// <summary>Current Battery status including the new battery charge of the agv after the change.
    /// </summary>
    [UsedImplicitly]
    public AgvBatteryStatus AgvBatteryStatus { get; init; }

    /// <summary>
    /// AutoMapper
    /// </summary>
    public AgvBatteryChargeChanged()
    {
        AgvBatteryStatus = null!;
    }

    /// <param name="oldBatteryCharge">Battery charge of the agv before the change.</param>
    /// <param name="agvBatteryStatus">Agv battery status.</param>
    public AgvBatteryChargeChanged(
        double oldBatteryCharge,
        AgvBatteryStatus agvBatteryStatus
    )
    {
        OldBatteryCharge = oldBatteryCharge;
        AgvBatteryStatus = agvBatteryStatus;
    }
}