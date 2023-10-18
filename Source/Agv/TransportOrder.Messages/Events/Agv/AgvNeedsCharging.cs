using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Event sent when an agv needs to be moved to a charging station, when battery level is below
/// agvSettings.BatteryLevelLow.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record AgvNeedsCharging(Guid CorrelationId) : CorrelatedBy<Guid>;