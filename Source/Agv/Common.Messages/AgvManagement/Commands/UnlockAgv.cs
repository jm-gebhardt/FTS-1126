using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Request an AGV to unlock
/// </summary>
[PublicAPI]
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