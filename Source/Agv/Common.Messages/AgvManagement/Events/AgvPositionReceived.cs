using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the current position of an AGV.
/// </summary>
[PublicAPI]
public record AgvPositionReceived : CorrelatedBy<Guid>, IAgvSerialNumber, IMessageCreatedDate, IWithAgvPosition
{
    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string SerialNumber { get; }

    /// <summary>
    /// Current AGV position
    /// </summary>
    public AgvPosition AgvPosition { get; }

    /// <inheritdoc />
    public DateTime MessageCreatedDate { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When true, the position update bypasses the significance and silence filters.
    /// Used by simulated drivers to ensure the snap-to-target final position reaches the UI,
    /// since simulators publish noise-free positions that can fall below the significance threshold.
    /// </summary>
    public bool ForcePublish { get; init; }

    /// <inheritdoc cref="AgvPositionReceived"/>
    public AgvPositionReceived(Guid correlationId, string serialNumber, AgvPosition agvPosition)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        AgvPosition = agvPosition;
    }
}