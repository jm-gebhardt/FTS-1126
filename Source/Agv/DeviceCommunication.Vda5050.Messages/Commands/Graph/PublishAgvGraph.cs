using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;

/// <summary>
/// Command to generate and publish the AGV graph for a specific AGV.
/// </summary>
[PublicAPI]
public record PublishAgvGraph(Guid AgvCorrelationId, string SerialNumber, string VehicleTypeId);