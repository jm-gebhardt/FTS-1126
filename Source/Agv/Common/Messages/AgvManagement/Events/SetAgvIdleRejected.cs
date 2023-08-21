using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// Agv reservation request was rejected
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record SetAgvIdleRejected : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="SetAgvIdleRejected"/>>
    /// <param name="correlationId"></param>
    public SetAgvIdleRejected(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}