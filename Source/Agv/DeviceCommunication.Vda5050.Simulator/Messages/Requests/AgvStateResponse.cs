using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides the response to a <see cref="RequestAgvState"/>.
/// </summary>
/// <param name="State">The latest VDA5050 state, or null if none has been published yet.</param>
[PublicAPI]
public record AgvStateResponse(MqttState? State);