using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting that the battery charge of an AGV has changed
    /// </summary>
    /// <param name="CorrelationId">The correlationId of the AGV</param>
    /// <param name="OldBatteryCharge">Battery charge of the AGV before the change</param>
    /// <param name="NewBatteryCharge">Battery charge of the AGV after the change</param>
    [JetBrains.Annotations.PublicAPI]
    public record AgvBatteryChargeChanged(Guid CorrelationId, double OldBatteryCharge, double NewBatteryCharge) : CorrelatedBy<Guid>;
}
