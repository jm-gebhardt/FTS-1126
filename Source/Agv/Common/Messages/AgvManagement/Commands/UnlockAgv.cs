using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Request an AGV to unlock
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record UnlockAgv : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="UnlockAgv"/>
    /// <param name="correlationId"></param>
    public UnlockAgv(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}