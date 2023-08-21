using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// Agv reservation request was rejected
/// </summary>
[JetBrains.Annotations.PublicAPI]
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