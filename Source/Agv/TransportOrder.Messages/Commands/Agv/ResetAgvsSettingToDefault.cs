namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
///  Provides a command to reset a settings property to default for the given agvs.
/// </summary>
/// <param name="CorrelationIds">The agv correlation ids.</param>
/// <param name="PropertyName">The property name.</param>
/// <param name="User">The user who updated the settings.</param>
public record ResetAgvsSettingToDefault(ICollection<Guid> CorrelationIds, string PropertyName, string User);