using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Settings;

/// <summary>
/// Provides a command to update the settings of an agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="UpdatedSettings">The updated settings.</param>
/// <param name="User">The user who updated the settings.</param>
public record UpdateAgvSettings(Guid CorrelationId, AgvSettingsReadModel UpdatedSettings, string User);