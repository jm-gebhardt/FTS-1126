using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Settings;

/// <summary>
/// Provides a command to update the default agv settings.
/// </summary>
/// <param name="UpdatedSettings">The updated settings.</param>
public record UpdateDefaultAgvSettings(AgvSettingsReadModel UpdatedSettings);