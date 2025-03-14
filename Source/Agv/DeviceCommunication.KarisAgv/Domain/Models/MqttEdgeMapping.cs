using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;

/// <summary>
/// Provides a mapping from an internal to a mqtt edge
/// </summary>
/// <param name="InternalEdge">The correlation id of the internally used edge.</param>
public record MqttEdgeMapping(Guid InternalEdge) : MqttEdge;