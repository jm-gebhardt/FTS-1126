using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Settings;

/// <summary>
/// Provides a command to update the settings of given agvs.
/// </summary>
/// <param name="CorrelationIds">The correlation id of the agvs.</param>
/// <param name="UpdatedSettings">The updated settings.</param>
public record UpdateAgvsSettings(ICollection<Guid> CorrelationIds, AgvSettingsReadModel UpdatedSettings);