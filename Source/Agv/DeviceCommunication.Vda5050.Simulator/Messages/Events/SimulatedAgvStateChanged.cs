using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Events;

/// <summary>
/// Represents an event that the VDA5050 state of a simulated agv has changed.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="State">The latest VDA5050 state of the agv.</param>
[PublicAPI]
public record SimulatedAgvStateChanged(Guid CorrelationId, MqttState State) : CorrelatedBy<Guid>;