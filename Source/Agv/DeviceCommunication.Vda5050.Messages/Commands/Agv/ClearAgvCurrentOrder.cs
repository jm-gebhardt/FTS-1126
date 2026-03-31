using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;

/// <summary>
/// Command to clear the current order reference from an AGV.
/// </summary>
[PublicAPI]
public record ClearAgvCurrentOrder(Guid AgvCorrelationId);