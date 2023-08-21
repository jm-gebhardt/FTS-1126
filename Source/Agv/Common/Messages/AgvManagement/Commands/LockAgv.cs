using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Request an AGV to lock
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record LockAgv : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId"></param>
    public LockAgv(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}