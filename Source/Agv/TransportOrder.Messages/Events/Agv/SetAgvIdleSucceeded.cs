using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Event that is raised when an agv was successfully set to idle.
/// </summary>
[PublicAPI]
public record SetAgvIdleSucceeded : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="SetAgvIdleSucceeded"/>>
    /// <param name="correlationId"></param>
    public SetAgvIdleSucceeded(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}