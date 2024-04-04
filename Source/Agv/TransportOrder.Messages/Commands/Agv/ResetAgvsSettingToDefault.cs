namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
///  Provides a command to reset a settings property to default for the given agvs.
/// </summary>
/// <param name="CorrelationIds">The agv correlation ids.</param>
/// <param name="PropertyName">The property name.</param>
public record ResetAgvsSettingToDefault(ICollection<Guid> CorrelationIds, string PropertyName);