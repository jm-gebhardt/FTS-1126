namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to set property of the setting of an agv to default.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Property">The setting property name.</param>
/// <param name="User">The user who reset the settings.</param>
public record ResetAgvSettingToDefault(Guid CorrelationId, string Property, string User);