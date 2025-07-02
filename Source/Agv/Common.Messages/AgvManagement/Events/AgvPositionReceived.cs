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
    public DateTime MessageCreatedDate { get; } = DateTime.UtcNow;

    /// <inheritdoc cref="AgvPositionReceived"/>
    public AgvPositionReceived(Guid correlationId, string serialNumber, AgvPosition agvPosition)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        AgvPosition = agvPosition;
    }
}