using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Settings;

/// <summary>
/// Provides a command to update the settings of an agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="UpdatedSettings">The updated settings.</param>
public record UpdateAgvSettings(Guid CorrelationId, AgvSettingsReadModel UpdatedSettings);