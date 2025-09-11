using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;

/// <summary>
/// Provides a command to update the default agv settings.
/// </summary>
/// <param name="UpdatedSettings">The updated settings.</param>
/// <param name="User">The user who updated the settings.</param>
public record UpdateDefaultAgvSettings(AgvSettingsReadModel UpdatedSettings, string User);