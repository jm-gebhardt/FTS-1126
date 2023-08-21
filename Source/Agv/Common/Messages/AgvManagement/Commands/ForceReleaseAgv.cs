using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Force an AGV to become idle again regardless of its current state
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record ForceReleaseAgv : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId"></param>
    public ForceReleaseAgv(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}