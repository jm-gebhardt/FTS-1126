using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages.SimulateAgv;

/// <summary>
/// Simulating Agv Error for Simulated Agv
/// </summary>
[PublicAPI]
[EntityName("amq.topic")]
[RoutingKey("simulatedagv.errors.simulate")]
public record MqttSimulateAgvError : CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlation Id of the Agv.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// List of errors. If null or empty it will remove any existing errors.
    /// </summary>
    public List<AgvError>? AgvErrors { get; init; } = new();

    /// <inheritdoc cref="MqttSimulateAgvError" />
    public MqttSimulateAgvError()
    {
    }
}